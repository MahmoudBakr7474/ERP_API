namespace WarehouseERP.Purchasing.Contracts;

/// <summary>A step in a purchasing document's history.</summary>
public enum PurchasingDocumentAction : byte
{
    /// <summary>The draft was created.</summary>
    Created = 1,

    /// <summary>It was submitted for approval.</summary>
    Submitted = 2,

    /// <summary>An approver sent it back to draft, with a reason.</summary>
    Rejected = 3,

    /// <summary>It was approved, or released without approval when its policy needs none.</summary>
    Approved = 4,

    /// <summary>It was abandoned.</summary>
    Cancelled = 5,

    /// <summary>What remained was closed, with a reason.</summary>
    Closed = 6,

    /// <summary>It was posted and numbered.</summary>
    Posted = 7,

    /// <summary>Goods were received against the order (the note names the goods receipt).</summary>
    GoodsReceived = 8,

    /// <summary>A supplier invoice for the order was posted (the note names it).</summary>
    Invoiced = 9,

    /// <summary>Goods of the order were returned to the supplier (the note names the return).</summary>
    Returned = 10,
}
