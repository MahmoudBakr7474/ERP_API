namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a sales order is in its life.</summary>
public enum SalesOrderStatus : byte
{
    /// <summary>Being prepared; lines may change; nothing reserved.</summary>
    Draft = 1,

    /// <summary>Waiting for an approver; nothing reserved yet.</summary>
    Submitted = 2,

    /// <summary>Numbered, prices fixed and every line's stock reserved.</summary>
    Confirmed = 3,

    /// <summary>Part of it was delivered; the rest is still reserved.</summary>
    PartiallyDelivered = 4,

    /// <summary>Everything ordered was delivered.</summary>
    Delivered = 5,

    /// <summary>What was not delivered was closed and its reservation released.</summary>
    Closed = 6,

    /// <summary>Abandoned before any delivery; any reservation released.</summary>
    Cancelled = 7,
}
