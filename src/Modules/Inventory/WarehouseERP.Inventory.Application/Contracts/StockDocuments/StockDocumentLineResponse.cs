using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <param name="Quantity">In the entered unit.</param>
/// <param name="BaseQuantity">In the product's base unit.</param>
/// <param name="UnitCost">Cost of one entered unit, when given.</param>
public sealed record StockDocumentLineResponse(
    Guid Id, int LineNumber, Guid ProductId, string Sku, string ProductNameAr, Guid ProductUnitId, Guid UnitId,
    string UnitCode, decimal ConversionFactor, decimal Quantity, decimal BaseQuantity, string BaseUnitCode,
    StockDirection Direction, decimal? UnitCost, Guid LocationId, string LocationCode);
