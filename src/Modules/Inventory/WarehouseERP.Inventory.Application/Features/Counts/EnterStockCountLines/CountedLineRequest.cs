namespace WarehouseERP.Inventory.Application.Features.Counts.EnterStockCountLines;

/// <param name="CountedQuantity">What was counted, in the product's base unit.</param>
/// <param name="UnitCost">Cost of one base unit for a surplus of a product never valued; optional otherwise.</param>
public sealed record CountedLineRequest(Guid ProductId, Guid LocationId, decimal CountedQuantity, decimal? UnitCost = null);
