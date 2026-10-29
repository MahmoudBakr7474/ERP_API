namespace WarehouseERP.Sales.Application.Features.Invoices.CancelSalesInvoice;

public sealed record CancelSalesInvoiceCommand(Guid InvoiceId, string RowVersion);
