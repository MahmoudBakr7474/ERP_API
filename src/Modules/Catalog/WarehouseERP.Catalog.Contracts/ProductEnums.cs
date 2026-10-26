namespace WarehouseERP.Catalog.Contracts;

/// <summary>Whether a product is held in stock.</summary>
public enum ProductType : byte
{
    /// <summary>Kept in warehouses and counted.</summary>
    StockItem = 1,

    /// <summary>Sold or bought but not kept in stock.</summary>
    NonStockItem = 2,

    /// <summary>A service; never in stock.</summary>
    Service = 3,
}

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
