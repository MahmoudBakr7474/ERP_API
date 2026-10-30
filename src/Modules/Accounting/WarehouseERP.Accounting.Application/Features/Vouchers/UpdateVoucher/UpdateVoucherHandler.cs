using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.UpdateVoucher;

// Drafts only, from the version the editor read; the voucher is rebuilt whole (lines, header and planned allocations).
public sealed partial class UpdateVoucherHandler(
    IJournalRepository journals, VoucherBuilder builder, JournalLineRules lineRules, ICurrentUser currentUser, TimeProvider clock,
    ILogger<UpdateVoucherHandler> logger)
    : ICommandHandler<UpdateVoucherCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateVoucherCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var journal = await journals.GetForUpdateAsync(command.JournalId, expected, ct);
        if (journal is null)
        {
            var denied = AccountingAccess.CompanyWide(currentUser);
            return denied.IsFailure ? denied.Error : JournalErrors.NotFound;
        }
        var access = JournalAccess.CanManage(currentUser, journal.JournalType);
        if (access.IsFailure)
            return access.Error;
        if (!journal.IsVoucher)
            return VoucherErrors.NotVoucher;
        var input = await builder.BuildAsync(new VoucherFields(journal.JournalType, command.PostingDate, command.Description, command.Reference,
            command.CashAccountId, command.CounterCashAccountId, command.PartyType, command.PartyId, command.PaymentMethod, command.Amount,
            command.WithholdingAmount, command.TaxAmount, command.DueDate, command.BranchId, command.Lines ?? [], command.Allocations ?? []), ct);
        if (input.IsFailure)
            return input.Error;
        var replaced = journal.ReplaceVoucher(input.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (replaced.IsFailure)
            return replaced.Error;
        var lines = await lineRules.CheckAsync(journal.Lines.Select(l => l.ToInput()).ToList(), journal.JournalType, ct);
        if (lines.IsFailure)
            return lines.Error;
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        VoucherUpdated(logger, journal.Id, journal.JournalType, journal.VoucherAmount);
        return Convert.ToBase64String(journal.RowVersion);
    }

    [LoggerMessage(EventId = 9051, Level = LogLevel.Information, Message = "Voucher {JournalId} ({Type}) updated, amount {Amount}")]
    private static partial void VoucherUpdated(ILogger logger, Guid journalId, JournalType type, decimal? amount);
}
