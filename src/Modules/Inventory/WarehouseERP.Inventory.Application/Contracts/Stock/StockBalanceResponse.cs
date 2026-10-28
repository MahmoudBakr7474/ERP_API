using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Stock;

/// <param name="Quantity">In the product's base unit.</param>
public sealed record StockBalanceResponse(
    Guid ProductId, string Sku, string ProductNameAr, string BaseUnitCode, Guid WarehouseId, string WarehouseCode,
    Guid LocationId, string LocationCode, StockStatus StockStatus, decimal Quantity, DateTime UpdatedAtUtc);
