namespace WarehouseERP.Sales.Application.Contracts.Invoices;

/// <summary>A posted invoice; a retry with the same Idempotency-Key gets the same answer.</summary>
public sealed record SalesInvoicePostedResponse(Guid InvoiceId, string Number, decimal TotalAmount, DateOnly DueDate, DateTime PostedAtUtc);
