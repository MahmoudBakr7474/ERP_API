namespace WarehouseERP.Inventory.Contracts;

/// <summary>One returned quantity, fit for resale.</summary>
/// <param name="DeliveredStockLineId">The posted delivery's stock document line it comes back from; its cost values it.</param>
/// <param name="ProductId">The delivered product.</param>
/// <param name="ProductUnitId">The product unit the quantity is in.</param>
/// <param name="Quantity">Quantity in that unit; never more than the delivery line still has unreturned.</param>
/// <param name="LocationId">The location of the warehouse it goes back into.</param>
public sealed record SalesReturnStockLine(Guid DeliveredStockLineId, Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid LocationId);
