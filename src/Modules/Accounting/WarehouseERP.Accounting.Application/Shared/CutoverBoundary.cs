using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Accounting.Application.Shared;

// A Purchasing, Sales or POS document and the stock document Inventory posts for it are stamped a moment apart in one
// transaction. The document's own timestamp decides where it belongs (the opening balances before the cutover's start, an
// automatic journal after it), while Inventory's valuation counts cost entries by theirs. A document stamped on one side of
// the start whose stock was stamped on the other is counted on its document's side: this correction is added to the
// valuation before the start.
public sealed class CutoverBoundary(
    IInventoryAccountingSource inventory, IPurchasingAccountingSource purchasing, ISalesAccountingSource sales, IPosAccountingSource pos)
{
    /// <summary>Far wider than one transaction: only documents stamped this close to the start are looked at.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public async Task<decimal> StockCorrectionAsync(DateTime cutoverStartUtc, CancellationToken ct)
    {
        var from = cutoverStartUtc - Window;
        var to = cutoverStartUtc + Window;
        var stock = new List<(Guid StockDocumentId, bool Before)>();
        foreach (var document in await purchasing.ListPostedAsync(from, to, ct))
        {
            var stockId = document.DocumentType switch
            {
                PurchasingDocumentType.GoodsReceipt => (await purchasing.GetGoodsReceiptAsync(document.Id, ct))?.StockDocumentId,
                PurchasingDocumentType.SupplierReturn => (await purchasing.GetSupplierReturnAsync(document.Id, ct))?.StockDocumentId,
                _ => null,
            };
            if (stockId is { } id)
                stock.Add((id, document.PostedAtUtc < cutoverStartUtc));
        }
        foreach (var document in await sales.ListPostedAsync(from, to, ct))
        {
            var stockId = document.DocumentType switch
            {
                SalesDocumentType.Delivery => (await sales.GetDeliveryAsync(document.Id, ct))?.StockDocumentId,
                SalesDocumentType.Return => (await sales.GetReturnAsync(document.Id, ct))?.StockDocumentId,
                _ => null,
            };
            if (stockId is { } id)
                stock.Add((id, document.PostedAtUtc < cutoverStartUtc));
        }
        foreach (var document in await pos.ListPostedAsync(from, to, ct))
        {
            var stockId = document.DocumentType switch
            {
                PosDocumentType.Sale => (await pos.GetSaleAsync(document.Id, ct))?.StockDocumentId,
                PosDocumentType.Return => (await pos.GetReturnAsync(document.Id, ct))?.StockDocumentId,
                _ => null,
            };
            if (stockId is { } id)
                stock.Add((id, document.OccurredAtUtc < cutoverStartUtc));
        }
        if (stock.Count == 0)
            return 0m;
        var postings = (await inventory.GetPostingsAsync(stock.Select(s => s.StockDocumentId).ToList(), ct)).ToDictionary(p => p.DocumentId);
        var correction = 0m;
        foreach (var (stockId, before) in stock)
        {
            if (!postings.TryGetValue(stockId, out var posting))
                continue;
            var stockBefore = posting.PostedAtUtc < cutoverStartUtc;
            if (before && !stockBefore)
                correction += posting.Value;
            else if (!before && stockBefore)
                correction -= posting.Value;
        }
        return correction;
    }
}
