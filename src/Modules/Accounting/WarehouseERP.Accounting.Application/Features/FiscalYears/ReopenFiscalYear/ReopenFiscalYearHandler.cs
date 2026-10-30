using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.FiscalYears.ReopenFiscalYear;

// accounting.periods.reopen with a reason, latest closed year first. Its closing journal stays in the ledger and is undone
// by a generated reversal (RJ) on the same date; the periods stay closed until they are reopened one by one. Closing the
// year again writes a new closing journal from the ledger as it then is.
public sealed partial class ReopenFiscalYearHandler(
    ITransactionRunner transactions, IFiscalYearRepository years, IJournalRepository journals, LedgerPosting posting,
    IAccountingLocks locks, ICurrentUser currentUser, TimeProvider clock, ILogger<ReopenFiscalYearHandler> logger)
    : ICommandHandler<ReopenFiscalYearCommand, string>
{
    public async Task<Result<string>> HandleAsync(ReopenFiscalYearCommand command, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        return await transactions.RunAsync(token => ReopenOnceAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> ReopenOnceAsync(ReopenFiscalYearCommand command, byte[] expected, CancellationToken ct)
    {
        if (!await locks.TryLockYearAsync(command.FiscalYearId, ct))
            return FiscalYearErrors.Busy;
        var year = await years.GetForUpdateAsync(command.FiscalYearId, expected, ct);
        if (year is null)
            return FiscalYearErrors.NotFound;
        if (!year.RowVersion.AsSpan().SequenceEqual(expected))
            return CommonErrors.ConcurrencyConflict;
        if (!year.IsClosed)
            return FiscalYearDomainErrors.YearNotClosed;
        if (await years.AnyClosedAfterAsync(year.EndDate, ct))
            return FiscalYearErrors.LaterYearClosed;

        var userId = currentUser.UserId;
        var now = clock.GetUtcNow().UtcDateTime;
        string? reversalNumber = null;
        if (year.ClosingJournalId is { } closingId)
        {
            var closing = await journals.GetForUpdateAsync(closingId, expectedRowVersion: null, ct)
                ?? throw new InvalidOperationException($"Fiscal year {year.Id} names closing journal {closingId}, which does not exist.");
            var created = JournalEntry.CreateGenerated(JournalType.Reversal, closing.PostingDate, $"عكس قيد إقفال السنة المالية {year.Name}",
                closing.CurrencyCode, closing.Mirrored(), closing.Id, userId, now);
            if (created.IsFailure)
                return created.Error;
            var slot = await posting.ReserveAsync(JournalType.Reversal, closing.PostingDate, intoClosedPeriod: true, ct);
            if (slot.IsFailure)
                return slot.Error;
            var reversal = created.Value;
            reversal.MarkPostedGenerated(slot.Value.Number, slot.Value.FiscalPeriodId, userId, now);
            journals.Add(reversal);
            closing.RecordReversed(slot.Value.Number.Number, userId, now);
            reversalNumber = slot.Value.Number.Number;
        }

        var reopened = year.Reopen(command.Reason, userId, now);
        if (reopened.IsFailure)
            return reopened.Error;
        var saved = await years.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        YearReopened(logger, year.Name, reversalNumber);
        return Convert.ToBase64String(year.RowVersion);
    }

    [LoggerMessage(EventId = 9034, Level = LogLevel.Information,
        Message = "Fiscal year {Name} reopened; its closing journal was reversed by {ReversalNumber}")]
    private static partial void YearReopened(ILogger logger, string name, string? reversalNumber);
}
