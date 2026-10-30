namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;

/// <summary>
/// Locks owned by the current transaction (released by its commit or rollback, never held across requests). Lock order:
/// request key → parties (by type and id) → cash accounts (by id) → fiscal year → period. Postings take their period's lock
/// shared; closing or reopening a period takes its year's and then its own exclusively, so a posting and a close never both
/// succeed on one period. Whatever changes a party's open items holds the party's lock; whatever posts to a cashbox or bank
/// holds its lock while it checks the balance. An operational document's journaling holds its accounting identity's lock
/// (taken first), and discovering documents holds the scan lock.
/// </summary>
public interface IAccountingLocks
{
    /// <summary>A customer's or supplier's lock (exclusive); false on a timeout.</summary>
    Task<bool> TryLockPartyAsync(WarehouseERP.Accounting.Contracts.PartyType partyType, Guid partyId, CancellationToken ct);

    /// <summary>A cashbox's or bank account's lock (exclusive); false on a timeout.</summary>
    Task<bool> TryLockCashAccountAsync(Guid cashAccountId, CancellationToken ct);

    /// <summary>The request key's lock; false when another attempt with the same key holds it too long.</summary>
    Task<bool> TryLockRequestAsync(Guid idempotencyKey, CancellationToken ct);

    /// <summary>An operational document's accounting identity's lock (exclusive); false on a timeout.</summary>
    Task<bool> TryLockSourceAsync(Guid sourcePostingId, CancellationToken ct);

    /// <summary>The lock of looking for new operational documents (exclusive); false on a timeout.</summary>
    Task<bool> TryLockSourceScanAsync(CancellationToken ct);

    /// <summary>A fiscal year's lock (exclusive); false on a timeout.</summary>
    Task<bool> TryLockYearAsync(Guid fiscalYearId, CancellationToken ct);

    /// <summary>A period's lock: shared to post into it, exclusive to close or reopen it; false on a timeout.</summary>
    Task<bool> TryLockPeriodAsync(Guid periodId, bool exclusive, CancellationToken ct);
}
