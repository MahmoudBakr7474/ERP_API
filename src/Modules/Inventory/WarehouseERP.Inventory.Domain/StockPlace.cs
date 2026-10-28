namespace WarehouseERP.Inventory.Domain;

/// <summary>A location together with its warehouse: where stock is.</summary>
public sealed record StockPlace(Guid WarehouseId, Guid LocationId);
