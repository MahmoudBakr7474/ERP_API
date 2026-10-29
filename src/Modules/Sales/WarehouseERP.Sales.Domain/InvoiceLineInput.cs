namespace WarehouseERP.Sales.Domain;

/// <summary>One invoice line: a quantity of an order line, in its unit, billed at the order line's agreed price.</summary>
/// <param name="Unit">The order line's product unit, with Catalog's fraction rules for checking the quantity.</param>
public sealed record InvoiceLineInput(Guid OrderLineId, ProductUnitFacts Unit, decimal Quantity);
