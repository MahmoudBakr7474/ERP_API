namespace WarehouseERP.Sales.Domain;

/// <summary>One picked quantity: an order line's product, in the order line's unit, from one location of the order's warehouse.</summary>
/// <param name="Unit">The order line's product unit, with Catalog's fraction rules for checking the quantity.</param>
public sealed record DeliveryLineInput(Guid OrderLineId, ProductUnitFacts Unit, decimal Quantity, Guid LocationId);
