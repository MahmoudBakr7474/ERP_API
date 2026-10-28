using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

/// <param name="DraftReference">DSRN-000123, its reference before it posts.</param>
public sealed record SupplierReturnSummaryResponse(
    Guid Id, string DraftReference, string? Number, SupplierReturnStatus Status, Guid GoodsReceiptId, string GoodsReceiptNumber,
    Guid PurchaseOrderId, string PurchaseOrderNumber, Guid SupplierId, string SupplierName, Guid WarehouseId,
    string WarehouseCode, string Reason, decimal CreditClaimTotalAmount, DateTime CreatedAtUtc, DateTime? PostedAtUtc);
