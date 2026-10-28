namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// How Purchasing puts the goods it receives into stock and takes the goods it returns out of stock. Inventory builds the
/// stock document (<see cref="StockDocumentType.PurchaseReceipt"/> or <see cref="StockDocumentType.PurchaseReturn"/>) and
/// posts it like any other: warehouse locks, count freezes, no negative stock, the moving average and its number.
/// Call it inside the caller's database transaction; the document joins the caller's unit of work and is saved by the
/// caller's save point, so the stock and the purchasing document commit or roll back together. The stock document keeps
/// the source document's id, which is unique: one stock document per receipt or return.
/// </summary>
public interface IPurchaseStockPosting
{
    /// <summary>Posts goods received: stock in, valued at each line's unit cost.</summary>
    Task<PurchaseStockOutcome> PostReceiptAsync(PurchaseStockRequest request, CancellationToken ct);

    /// <summary>Posts goods returned to the supplier: stock out at the current moving average cost.</summary>
    Task<PurchaseStockOutcome> PostReturnAsync(PurchaseStockRequest request, CancellationToken ct);
}
