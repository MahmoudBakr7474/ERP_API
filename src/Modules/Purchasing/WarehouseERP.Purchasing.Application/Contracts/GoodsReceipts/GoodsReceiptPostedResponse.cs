namespace WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;

/// <summary>What receiving did; a retry with the same Idempotency-Key gets the same answer.</summary>
/// <param name="Number">GRN-…</param>
/// <param name="StockDocumentId">The PurchaseReceipt stock document Inventory posted.</param>
/// <param name="StockDocumentNumber">PRC-…</param>
public sealed record GoodsReceiptPostedResponse(
    Guid Id, string Number, Guid StockDocumentId, string StockDocumentNumber, DateTime ReceivedAtUtc);
