namespace WarehouseERP.Sales.Application.Contracts.Invoices;

/// <summary>One invoice line as the client sends it: a delivered quantity of an order line, billed at its agreed price.</summary>
/// <param name="Quantity">In the order line's unit; at most what was delivered and not invoiced or returned before invoicing yet.</param>
public sealed record SalesInvoiceLineRequest(Guid OrderLineId, decimal Quantity);
