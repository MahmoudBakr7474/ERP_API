namespace WarehouseERP.Inventory.Contracts;

/// <summary>One order line to reserve.</summary>
/// <param name="SourceLineId">The sales order line; at most one reservation per line.</param>
/// <param name="ProductId">An untracked stock item.</param>
/// <param name="ProductUnitId">The product unit the quantity is in.</param>
/// <param name="Quantity">Quantity in that unit; it must convert exactly to base units.</param>
public sealed record StockReservationLine(Guid SourceLineId, Guid ProductId, Guid ProductUnitId, decimal Quantity);
