using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// The part of every posting that touches the calendar, inside the caller's transaction: find the period of the journal's
// date, take its lock shared (a close or reopen takes it exclusively, so the two never overlap), read its state again under
// the lock, and number the journal in its type's series of that fiscal year. A rolled-back posting gives the number back.
public sealed class LedgerPosting(IFiscalYearRepository years, IAccountingLocks locks, IDocumentNumbers numbers)
{
    /// <summary>
    /// The open period and number for a journal of this type and date, or why it can't post. Only the year-end close and its
    /// reversal (<paramref name="intoClosedPeriod"/>) post into the closed last period of the year they close or reopen.
    /// </summary>
    public async Task<Result<PostingSlot>> ReserveAsync(JournalType type, DateOnly postingDate, bool intoClosedPeriod, CancellationToken ct)
    {
        var period = await years.FindPeriodAsync(postingDate, ct);
        if (period is null)
            return JournalErrors.PeriodMissing(postingDate);
        if (!await locks.TryLockPeriodAsync(period.PeriodId, exclusive: false, ct))
            return JournalErrors.PeriodBusy;
        var status = await years.PeriodStatusAsync(period.PeriodId, ct)
            ?? throw new InvalidOperationException($"Period {period.PeriodId} disappeared while it was locked.");
        if (!intoClosedPeriod && status != FiscalPeriodStatus.Open)
            return JournalErrors.PeriodClosed(postingDate);
        var sequence = await numbers.NextAsync(type, period.FiscalYearId, ct);
        return new PostingSlot(period.PeriodId,
            new DocumentNumber(JournalNumbers.Format(type, period.FiscalYearStart.Year, sequence), period.FiscalYearId, sequence));
    }
}
