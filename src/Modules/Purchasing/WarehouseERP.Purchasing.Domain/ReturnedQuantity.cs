namespace WarehouseERP.Purchasing.Domain;

/// <summary>Goods of one order line going back to the supplier, in the line's unit.</summary>
public sealed record ReturnedQuantity(Guid OrderLineId, decimal Quantity);
