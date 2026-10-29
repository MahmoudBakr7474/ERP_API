namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// How POS takes the goods a checkout sold out of stock and puts goods a customer returned back. Inventory builds the stock
/// document (<see cref="StockDocumentType.PosSale"/> or <see cref="StockDocumentType.PosReturn"/>) and posts it like any
/// other: warehouse locks, count freezes, location balances, no negative stock, the moving average and its number. A sale
/// takes only stock nobody reserved, so it never sells what a sales order holds. Call it inside the caller's database
/// transaction; the document joins the caller's unit of work, so the stock and the POS document commit or roll back
/// together. The stock document keeps the source document's id, which is unique.
/// </summary>
public interface IPosStockPosting
{
    /// <summary>
    /// Posts a sale: each line leaves its location at the moving average, from stock that is on hand and not reserved, in
    /// an active physical warehouse that allows selling.
    /// </summary>
    Task<PosStockOutcome> PostSaleAsync(PosSaleStockRequest request, CancellationToken ct);

    /// <summary>Posts a return: each line comes back into sellable stock at the cost its sale line left with.</summary>
    Task<PosStockOutcome> PostReturnAsync(PosReturnStockRequest request, CancellationToken ct);
}
