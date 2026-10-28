namespace WarehouseERP.Inventory.Contracts;

/// <summary>A step in a stock count's history.</summary>
public enum StockCountAction : byte
{
    /// <summary>The count was created.</summary>
    Created = 1,

    /// <summary>Its scope was frozen and snapshotted.</summary>
    Started = 2,

    /// <summary>It was submitted for approval.</summary>
    Submitted = 3,

    /// <summary>An approver sent it back for counting, with a reason.</summary>
    Rejected = 4,

    /// <summary>An approver approved it.</summary>
    Approved = 5,

    /// <summary>Its differences posted and the freeze was released.</summary>
    Completed = 6,

    /// <summary>It was abandoned.</summary>
    Cancelled = 7,
}
