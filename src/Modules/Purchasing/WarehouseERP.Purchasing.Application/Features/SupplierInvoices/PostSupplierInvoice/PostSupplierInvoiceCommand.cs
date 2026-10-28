namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.PostSupplierInvoice;

/// <summary>Posts a draft whose prices match the order's.</summary>
/// <param name="IdempotencyKey">A new GUID per posting click, sent again unchanged when the client retries.</param>
public sealed record PostSupplierInvoiceCommand(Guid SupplierInvoiceId, string RowVersion, Guid IdempotencyKey);
