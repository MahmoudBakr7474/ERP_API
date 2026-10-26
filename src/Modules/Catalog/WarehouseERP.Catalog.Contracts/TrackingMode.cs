namespace WarehouseERP.Catalog.Contracts;

/// <summary>How individual units of a stock item are told apart.</summary>
public enum TrackingMode : byte
{
    /// <summary>Not tracked beyond quantity.</summary>
    None = 0,

    /// <summary>By batch (lot), with optional expiry.</summary>
    Batch = 1,

    /// <summary>By serial number, one per unit.</summary>
    Serial = 2,
}
