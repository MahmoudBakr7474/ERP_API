namespace WarehouseERP.Inventory.Contracts;

/// <summary>A step in a stock document's history.</summary>
public enum StockDocumentAction : byte
{
    /// <summary>The draft was created.</summary>
    Created = 1,

    /// <summary>The draft was submitted for approval.</summary>
    Submitted = 2,

    /// <summary>An approver sent it back to draft, with a reason.</summary>
    Rejected = 3,

    /// <summary>An approver approved it.</summary>
    Approved = 4,

    /// <summary>It was applied to stock and numbered.</summary>
    Posted = 5,

    /// <summary>It was abandoned.</summary>
    Cancelled = 6,
}
