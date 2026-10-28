using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

/// <param name="DraftReference">DSIN-000123, its reference before it posts.</param>
/// <param name="RequiresApproval">True while its prices differ from the order's.</param>
public sealed record SupplierInvoiceSummaryResponse(
    Guid Id, string DraftReference, string? Number, SupplierInvoiceStatus Status, Guid SupplierId, string SupplierName,
    Guid PurchaseOrderId, string PurchaseOrderNumber, Guid WarehouseId, string WarehouseCode, string SupplierInvoiceNumber,
    DateOnly InvoiceDate, DateOnly DueDate, string CurrencyCode, decimal TotalAmount, decimal PriceVarianceAmount,
    bool RequiresApproval, DateTime CreatedAtUtc, DateTime? PostedAtUtc);
