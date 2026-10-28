namespace WarehouseERP.Purchasing.Domain;

/// <summary>Goods received for one order line.</summary>
/// <param name="Quantity">In the order line's unit.</param>
/// <param name="LocationId">The location of the order's warehouse they go into.</param>
public sealed record ReceiptLineInput(Guid OrderLineId, decimal Quantity, Guid LocationId);
