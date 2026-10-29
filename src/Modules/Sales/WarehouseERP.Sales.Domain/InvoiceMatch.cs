namespace WarehouseERP.Sales.Domain;

/// <summary>What a posted invoice line bills against its order line.</summary>
public sealed record InvoiceMatch(Guid OrderLineId, decimal Quantity, decimal NetAmount, decimal TaxAmount);
