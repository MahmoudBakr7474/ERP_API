using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

/// <param name="StockDocumentId">The PurchaseReturn stock document Inventory posted, once approved.</param>
/// <param name="CreditClaimTotalAmount">A pending supplier credit claim for future Accounting, not a posted credit note.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record SupplierReturnDetailsResponse(
    Guid Id, string DraftReference, string? Number, SupplierReturnStatus Status, Guid GoodsReceiptId, string GoodsReceiptNumber,
    Guid PurchaseOrderId, string PurchaseOrderNumber, Guid SupplierId, string SupplierCode, string SupplierName, Guid WarehouseId,
    string WarehouseCode, Guid BranchId, string CurrencyCode, string Reason, string? Notes, Guid? StockDocumentId,
    string? StockDocumentNumber, decimal CreditClaimNetAmount, decimal CreditClaimTaxAmount, decimal CreditClaimTotalAmount,
    DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy, DateTime? PostedAtUtc,
    string RowVersion, IReadOnlyList<SupplierReturnLineResponse> Lines, IReadOnlyList<DocumentEventResponse> Events);
