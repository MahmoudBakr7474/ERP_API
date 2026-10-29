namespace WarehouseERP.Sales.Contracts;

/// <summary>Where a delivery note is in its life.</summary>
public enum SalesDeliveryStatus : byte
{
    /// <summary>A picking list: lines and locations may change; stock is not touched.</summary>
    Draft = 1,

    /// <summary>Numbered; the goods left stock out of the order's reservation.</summary>
    Posted = 2,

    /// <summary>Abandoned before posting.</summary>
    Cancelled = 3,
}
