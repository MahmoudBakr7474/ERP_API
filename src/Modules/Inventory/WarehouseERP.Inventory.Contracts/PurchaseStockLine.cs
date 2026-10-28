namespace WarehouseERP.Inventory.Contracts;

/// <summary>One product of a purchase receipt or return.</summary>
/// <param name="ProductId">An untracked stock item.</param>
/// <param name="ProductUnitId">The product unit the quantity is in.</param>
/// <param name="Quantity">Quantity in that unit.</param>
/// <param name="LocationId">The warehouse location the goods go into or come out of.</param>
/// <param name="UnitCost">For a receipt, the cost of one such unit in the base currency; null for a return.</param>
public sealed record PurchaseStockLine(Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid LocationId, decimal? UnitCost);
