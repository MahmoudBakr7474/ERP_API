using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

/// <param name="RequiresApproval">The policy the order was created under.</param>
/// <param name="CreatedByUserId">Its creator, who may also approve it.</param>
/// <param name="Documents">Its goods receipts, supplier invoices and returns, oldest first.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record PurchaseOrderDetailsResponse(
    Guid Id, string DraftReference, string? Number, PurchaseOrderStatus Status, Guid SupplierId, string SupplierCode,
    string SupplierName, Guid WarehouseId, string WarehouseCode, Guid BranchId, string CurrencyCode, bool RequiresApproval,
    DateOnly? ExpectedDeliveryDate, string? SupplierReference, int PaymentTermDays, string? Notes, decimal GrossAmount,
    decimal DiscountAmount, decimal NetAmount, decimal TaxAmount, decimal TotalAmount, string? ClosedReason,
    DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy,
    DateTime? ApprovedAtUtc, string RowVersion, IReadOnlyList<PurchaseOrderLineResponse> Lines,
    IReadOnlyList<LinkedDocumentResponse> Documents, IReadOnlyList<DocumentEventResponse> Events);
