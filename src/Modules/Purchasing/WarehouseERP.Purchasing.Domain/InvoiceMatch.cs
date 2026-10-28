namespace WarehouseERP.Purchasing.Domain;

/// <summary>What a posted invoice line bills for one order line.</summary>
public sealed record InvoiceMatch(Guid OrderLineId, decimal Quantity, decimal NetAmount, decimal TaxAmount);
