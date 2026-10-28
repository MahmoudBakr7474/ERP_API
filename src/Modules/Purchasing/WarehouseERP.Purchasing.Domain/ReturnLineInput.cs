namespace WarehouseERP.Purchasing.Domain;

/// <summary>A return line as entered.</summary>
/// <param name="ReceiptLineId">The goods receipt line the goods came in on.</param>
/// <param name="Quantity">In the order line's unit.</param>
/// <param name="LocationId">Where the goods leave from; the location they were received into when left out.</param>
public sealed record ReturnLineInput(Guid ReceiptLineId, decimal Quantity, Guid? LocationId);
