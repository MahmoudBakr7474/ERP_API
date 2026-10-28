using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>A line a transfer or a count produces, in the product's base unit at one place.</summary>
/// <param name="ProductUnitId">The product's base product unit.</param>
/// <param name="UnitId">The base unit of measure.</param>
/// <param name="UnitCost">Cost of one base unit for stock coming in; null to value it at the average.</param>
public sealed record GeneratedLineInput(
    Guid ProductId, Guid ProductUnitId, Guid UnitId, decimal BaseQuantity, StockDirection Direction, decimal? UnitCost, StockPlace Place);
