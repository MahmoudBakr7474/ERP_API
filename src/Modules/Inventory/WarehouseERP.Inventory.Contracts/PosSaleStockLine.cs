namespace WarehouseERP.Inventory.Contracts;

/// <summary>One sold quantity of a POS sale.</summary>
/// <param name="ProductId">An untracked stock item.</param>
/// <param name="ProductUnitId">The product unit the quantity is in.</param>
/// <param name="Quantity">Quantity in that unit; it must convert exactly to base units.</param>
/// <param name="LocationId">The terminal's selling location, in the sale's warehouse.</param>
public sealed record PosSaleStockLine(Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid LocationId);
