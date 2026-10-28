namespace WarehouseERP.Purchasing.Contracts;

/// <summary>Where a purchase order is in its life.</summary>
public enum PurchaseOrderStatus : byte
{
    /// <summary>Being prepared; its lines can change.</summary>
    Draft = 1,

    /// <summary>Waiting for approval.</summary>
    Submitted = 2,

    /// <summary>Approved and numbered; goods may be received.</summary>
    Approved = 3,

    /// <summary>Some goods were received; the rest may still come.</summary>
    PartiallyReceived = 4,

    /// <summary>Every line was received in full. Returns never reopen it.</summary>
    Received = 5,

    /// <summary>Closed by hand after a partial receipt; the rest will not be received.</summary>
    Closed = 6,

    /// <summary>Abandoned before anything was received.</summary>
    Cancelled = 7,
}
