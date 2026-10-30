namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;

/// <summary>
/// Locks owned by the current transaction (released by its commit or rollback, never held across requests). Lock order:
/// request key → fiscal year → period. Postings take their period's lock shared; closing or reopening a period takes its
/// year's and then its own exclusively, so a posting and a close never both succeed on one period.
/// </summary>
public interface IAccountingLocks
{
    /// <summary>The request key's lock; false when another attempt with the same key holds it too long.</summary>
    Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct);

    /// <summary>A fiscal year's lock (exclusive); false on a timeout.</summary>
    Task<bool> TryLockYearAsync(Guid fiscalYearId, CancellationToken ct);

    /// <summary>A period's lock: shared to post into it, exclusive to close or reopen it; false on a timeout.</summary>
    Task<bool> TryLockPeriodAsync(Guid periodId, bool exclusive, CancellationToken ct);
}
