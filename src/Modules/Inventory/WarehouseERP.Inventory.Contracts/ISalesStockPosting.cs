namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// How Sales takes delivered goods out of stock and puts returned goods back. Inventory builds the stock document
/// (<see cref="StockDocumentType.SalesDelivery"/> or <see cref="StockDocumentType.SalesReturn"/>) and posts it like any
/// other: warehouse locks, count freezes, location balances, no negative stock, the moving average and its number. Call
/// it inside the caller's database transaction; the document joins the caller's unit of work, so the stock and the sales
/// document commit or roll back together. The stock document keeps the source document's id, which is unique.
/// </summary>
public interface ISalesStockPosting
{
    /// <summary>
    /// Posts a delivery: each line leaves its location at the moving average and consumes its order line's reservation,
    /// lowering on hand and reserved stock together. The stock must physically be there, even in an over-reserved
    /// warehouse.
    /// </summary>
    Task<SalesStockOutcome> PostDeliveryAsync(SalesDeliveryStockRequest request, CancellationToken ct);

    /// <summary>Posts a return: each line comes back into sellable stock at the cost its delivery line left with.</summary>
    Task<SalesStockOutcome> PostReturnAsync(SalesReturnStockRequest request, CancellationToken ct);
}
