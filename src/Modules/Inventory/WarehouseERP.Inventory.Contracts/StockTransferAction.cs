namespace WarehouseERP.Inventory.Contracts;

/// <summary>A step in a transfer's history.</summary>
public enum StockTransferAction : byte
{
    /// <summary>The draft was created.</summary>
    Created = 1,

    /// <summary>The draft was submitted for approval.</summary>
    Submitted = 2,

    /// <summary>An approver sent it back to draft, with a reason.</summary>
    Rejected = 3,

    /// <summary>An approver approved the dispatch.</summary>
    Approved = 4,

    /// <summary>The stock left the source warehouse.</summary>
    Dispatched = 5,

    /// <summary>Stock arrived in the destination warehouse.</summary>
    Received = 6,

    /// <summary>The outstanding quantity was sent for a loss adjustment.</summary>
    ClosingRequested = 7,

    /// <summary>The loss adjustment was cancelled; receiving goes on.</summary>
    ClosingAborted = 8,

    /// <summary>The loss adjustment posted and the transfer closed.</summary>
    Closed = 9,

    /// <summary>It was abandoned.</summary>
    Cancelled = 10,
}
