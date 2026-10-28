namespace WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;

public sealed record GoodsReceiptSummaryResponse(
    Guid Id, string Number, Guid PurchaseOrderId, string PurchaseOrderNumber, Guid SupplierId, string SupplierName,
    Guid WarehouseId, string WarehouseCode, string StockDocumentNumber, string? SupplierDeliveryNote, DateTime ReceivedAtUtc,
    string? ReceivedBy);
