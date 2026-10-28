using WarehouseERP.Purchasing.Application.Contracts.Documents;

namespace WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;

/// <param name="StockDocumentId">The PurchaseReceipt stock document Inventory posted (it names this receipt back).</param>
/// <param name="Returns">Supplier returns drawn on this receipt.</param>
public sealed record GoodsReceiptDetailsResponse(
    Guid Id, string Number, Guid PurchaseOrderId, string PurchaseOrderNumber, Guid SupplierId, string SupplierCode,
    string SupplierName, Guid WarehouseId, string WarehouseCode, Guid BranchId, string CurrencyCode, string? SupplierDeliveryNote,
    string? Notes, Guid StockDocumentId, string StockDocumentNumber, DateTime ReceivedAtUtc, Guid? ReceivedByUserId,
    string? ReceivedBy, IReadOnlyList<GoodsReceiptLineResponse> Lines, IReadOnlyList<LinkedDocumentResponse> Returns);
