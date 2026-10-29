namespace WarehouseERP.Inventory.Contracts;

/// <summary>One picked quantity of a delivery.</summary>
/// <param name="ReservationLineId">The sales order line whose reservation it consumes.</param>
/// <param name="ProductId">The order line's product.</param>
/// <param name="ProductUnitId">The product unit the quantity is in.</param>
/// <param name="Quantity">Quantity in that unit.</param>
/// <param name="LocationId">The location of the order's warehouse it is picked from.</param>
public sealed record SalesDeliveryStockLine(Guid ReservationLineId, Guid ProductId, Guid ProductUnitId, decimal Quantity, Guid LocationId);
