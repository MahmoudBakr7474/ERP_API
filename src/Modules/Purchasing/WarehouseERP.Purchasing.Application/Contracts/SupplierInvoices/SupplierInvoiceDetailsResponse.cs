using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

/// <param name="SupplierInvoiceNumber">The supplier's number as printed; unique per supplier once normalized.</param>
/// <param name="TotalAmount">Net plus tax: the liability to the supplier once posted.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SupplierInvoiceDetailsResponse(
    Guid Id, string DraftReference, string? Number, SupplierInvoiceStatus Status, Guid SupplierId, string SupplierCode,
    string SupplierName, Guid PurchaseOrderId, string PurchaseOrderNumber, Guid WarehouseId, string WarehouseCode, Guid BranchId,
    string CurrencyCode, string SupplierInvoiceNumber, DateOnly InvoiceDate, DateOnly DueDate, string? Notes,
    bool RequiresApproval, decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount,
    decimal PriceVarianceAmount, DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy,
    string? ApprovedBy, DateTime? PostedAtUtc, string? PostedBy, string RowVersion, IReadOnlyList<SupplierInvoiceLineResponse> Lines,
    IReadOnlyList<DocumentEventResponse> Events);
