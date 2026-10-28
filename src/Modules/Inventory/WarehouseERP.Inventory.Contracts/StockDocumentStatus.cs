namespace WarehouseERP.Inventory.Contracts;

/// <summary>Where a stock document is in its life.</summary>
public enum StockDocumentStatus : byte
{
    /// <summary>Being prepared; its lines can change.</summary>
    Draft = 1,

    /// <summary>Waiting for approval; approving posts it, rejecting returns it to draft.</summary>
    Submitted = 2,

    /// <summary>Applied to stock and valuation, numbered and immutable.</summary>
    Posted = 3,

    /// <summary>Abandoned before posting.</summary>
    Cancelled = 4,
}
