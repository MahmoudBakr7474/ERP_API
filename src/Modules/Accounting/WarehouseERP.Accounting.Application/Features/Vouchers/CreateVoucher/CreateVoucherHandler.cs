using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Vouchers.CreateVoucher;

// The permission of the voucher's type (receivables, payables or treasury), its accounts resolved from data, its lines built
// and checked like any journal's. It copies the voucher approval policy now.
public sealed partial class CreateVoucherHandler(
    IJournalRepository journals, VoucherBuilder builder, JournalLineRules lineRules, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CreateVoucherHandler> logger)
    : ICommandHandler<CreateVoucherCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateVoucherCommand command, CancellationToken ct)
    {
        var access = JournalAccess.CanManage(currentUser, command.Type);
        if (access.IsFailure)
            return access.Error;
        var input = await builder.BuildAsync(new VoucherFields(command.Type, command.PostingDate, command.Description, command.Reference,
            command.CashAccountId, command.CounterCashAccountId, command.PartyType, command.PartyId, command.PaymentMethod, command.Amount,
            command.WithholdingAmount, command.TaxAmount, command.DueDate, command.BranchId, command.Lines ?? [], command.Allocations ?? []), ct);
        if (input.IsFailure)
            return input.Error;
        var created = JournalEntry.CreateVoucher(input.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        var lines = await lineRules.CheckAsync(created.Value.Lines.Select(l => l.ToInput()).ToList(), created.Value.JournalType, ct);
        if (lines.IsFailure)
            return lines.Error;
        journals.Add(created.Value);
        var saved = await journals.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        VoucherDrafted(logger, created.Value.Id, created.Value.JournalType, created.Value.VoucherAmount);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 9050, Level = LogLevel.Information, Message = "Voucher {JournalId} ({Type}) drafted for {Amount}")]
    private static partial void VoucherDrafted(ILogger logger, Guid journalId, WarehouseERP.Accounting.Contracts.JournalType type, decimal? amount);
}
