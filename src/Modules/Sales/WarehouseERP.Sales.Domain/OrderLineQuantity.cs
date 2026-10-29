namespace WarehouseERP.Sales.Domain;

/// <summary>A quantity of an order line, in the line's unit: delivered, or returned.</summary>
public sealed record OrderLineQuantity(Guid OrderLineId, decimal Quantity);
