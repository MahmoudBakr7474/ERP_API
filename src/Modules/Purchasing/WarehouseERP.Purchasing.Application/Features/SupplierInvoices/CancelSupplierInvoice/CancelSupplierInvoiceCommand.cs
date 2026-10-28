namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CancelSupplierInvoice;

/// <summary>Abandons a draft or submitted invoice; its supplier invoice number becomes free again.</summary>
public sealed record CancelSupplierInvoiceCommand(Guid SupplierInvoiceId, string RowVersion);
