namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.ApproveSupplierInvoice;

/// <summary>Approves a submitted invoice with price differences, which posts it.</summary>
/// <param name="IdempotencyKey">A new GUID per approval click, sent again unchanged when the client retries.</param>
public sealed record ApproveSupplierInvoiceCommand(Guid SupplierInvoiceId, string RowVersion, Guid IdempotencyKey);
