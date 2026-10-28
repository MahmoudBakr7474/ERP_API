namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.SubmitSupplierInvoice;

/// <summary>Sends a draft whose prices differ from the order's for approval.</summary>
public sealed record SubmitSupplierInvoiceCommand(Guid SupplierInvoiceId, string RowVersion);
