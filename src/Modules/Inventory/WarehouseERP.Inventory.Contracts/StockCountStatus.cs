namespace WarehouseERP.Inventory.Contracts;

/// <summary>Where a stock count is in its life.</summary>
public enum StockCountStatus : byte
{
    /// <summary>Its scope is chosen; nothing is frozen yet.</summary>
    Draft = 1,

    /// <summary>Its scope is frozen and its expected quantities are snapshotted; counted quantities are being entered.</summary>
    Counting = 2,

    /// <summary>Every line is counted; it waits for approval.</summary>
    Submitted = 3,

    /// <summary>Approved: its differences posted as a count adjustment and the freeze released.</summary>
    Completed = 4,

    /// <summary>Abandoned; the freeze released.</summary>
    Cancelled = 5,
}
