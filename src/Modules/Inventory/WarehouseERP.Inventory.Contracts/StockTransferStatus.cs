namespace WarehouseERP.Inventory.Contracts;

/// <summary>Where a transfer is in its life.</summary>
public enum StockTransferStatus : byte
{
    /// <summary>Being prepared; its lines can change.</summary>
    Draft = 1,

    /// <summary>Waiting for approval to dispatch.</summary>
    Submitted = 2,

    /// <summary>Dispatched: the stock sits in the transfer's transit location.</summary>
    InTransit = 3,

    /// <summary>Some of the stock has arrived; the rest is still in transit.</summary>
    PartiallyReceived = 4,

    /// <summary>The outstanding quantity waits for an approved loss adjustment; nothing more can be received.</summary>
    Closing = 5,

    /// <summary>Everything arrived.</summary>
    Received = 6,

    /// <summary>Done, with the outstanding quantity written off by an approved adjustment.</summary>
    Closed = 7,

    /// <summary>Abandoned before dispatch.</summary>
    Cancelled = 8,
}
