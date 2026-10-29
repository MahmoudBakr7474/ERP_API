namespace WarehouseERP.Sales.Application.Contracts.Deliveries;

/// <summary>One picked quantity as the client sends it.</summary>
/// <param name="OrderLineId">A line of the delivery's order; several delivery lines may pick it from different locations.</param>
/// <param name="Quantity">In the order line's unit; all of an order line's picks together at most what remains to deliver.</param>
/// <param name="LocationId">A location of the order's warehouse to pick from; its DEFAULT location when left out.</param>
public sealed record DeliveryLineRequest(Guid OrderLineId, decimal Quantity, Guid? LocationId = null);
