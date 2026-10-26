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
