namespace WarehouseERP.Accounting.Contracts;

/// <summary>Where the journal of an operational document stands.</summary>
public enum SourcePostingStatus : byte
{
    /// <summary>Found and waiting for its journal.</summary>
    Pending = 1,

    /// <summary>Its journal posted.</summary>
    Posted = 2,

    /// <summary>Its journal could not post; the error says why, and it is tried again until it posts.</summary>
    Failed = 3,

    /// <summary>It moved no amount the ledger records (nothing of value), so no journal was written.</summary>
    NothingToPost = 4,
}
