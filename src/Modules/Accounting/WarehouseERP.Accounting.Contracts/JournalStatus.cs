namespace WarehouseERP.Accounting.Contracts;

/// <summary>Where a journal is in its life. Only posted journals count in balances, and a posted journal never changes.</summary>
public enum JournalStatus : byte
{
    /// <summary>Being written; it may change.</summary>
    Draft = 1,

    /// <summary>Waiting for an approver.</summary>
    Submitted = 2,

    /// <summary>In the ledger under its number; immutable, corrected only by a reversal or another journal.</summary>
    Posted = 3,

    /// <summary>Dropped before posting.</summary>
    Cancelled = 4,
}
