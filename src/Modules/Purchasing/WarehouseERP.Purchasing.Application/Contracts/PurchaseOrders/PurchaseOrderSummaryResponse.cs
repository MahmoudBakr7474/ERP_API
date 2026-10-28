using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

/// <param name="DraftReference">DPO-000123, its reference before it is numbered.</param>
/// <param name="Number">PO-…, once approved.</param>
public sealed record PurchaseOrderSummaryResponse(
    Guid Id, string DraftReference, string? Number, PurchaseOrderStatus Status, Guid SupplierId, string SupplierCode,
    string SupplierName, Guid WarehouseId, string WarehouseCode, string CurrencyCode, decimal TotalAmount,
    DateOnly? ExpectedDeliveryDate, DateTime CreatedAtUtc, string? CreatedBy, DateTime? ApprovedAtUtc);
