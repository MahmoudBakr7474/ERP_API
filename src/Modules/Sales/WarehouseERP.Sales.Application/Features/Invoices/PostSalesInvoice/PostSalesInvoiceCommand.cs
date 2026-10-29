namespace WarehouseERP.Sales.Application.Features.Invoices.PostSalesInvoice;

/// <param name="RowVersion">The version of the draft the user saw.</param>
/// <param name="IdempotencyKey">From the Idempotency-Key header: a new GUID per click, sent again unchanged on a retry.</param>
public sealed record PostSalesInvoiceCommand(Guid InvoiceId, string RowVersion, Guid IdempotencyKey);
