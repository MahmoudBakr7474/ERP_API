namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.RejectSupplierInvoice;

/// <param name="Reason">Why it goes back to draft; kept in its history.</param>
public sealed record RejectSupplierInvoiceCommand(Guid SupplierInvoiceId, string Reason, string RowVersion);
