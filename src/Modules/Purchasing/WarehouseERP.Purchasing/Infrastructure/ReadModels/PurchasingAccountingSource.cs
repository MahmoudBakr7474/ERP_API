using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

// What Accounting may learn about posted purchasing documents, read without locks. Goods received not invoiced is valued
// per order line (UninvoicedReceipts): a document's amount is the change it made to its lines' value, from the uninvoiced
// quantity left by every earlier receipt, invoice and return of those lines (in posting order) to what it left. Posting
// order is the documents' own timestamps, then their ids; each of them changed the order in its transaction, so they
// can't interleave.
internal sealed class PurchasingAccountingSource(DbContext db) : IPurchasingAccountingSource
{
    public async Task<IReadOnlyList<PurchasingPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var receipts = await db.Set<GoodsReceipt>().AsNoTracking()
            .Where(x => x.ReceivedAtUtc >= fromUtc && x.ReceivedAtUtc < toUtc)
            .Select(x => new PurchasingPostedDocument(x.Id, PurchasingDocumentType.GoodsReceipt, x.Number, x.ReceivedAtUtc))
            .ToListAsync(ct);
        var invoices = await db.Set<SupplierInvoice>().AsNoTracking()
            .Where(x => x.Status == SupplierInvoiceStatus.Posted && x.PostedAtUtc >= fromUtc && x.PostedAtUtc < toUtc)
            .Select(x => new PurchasingPostedDocument(x.Id, PurchasingDocumentType.SupplierInvoice, x.Number!, x.PostedAtUtc!.Value))
            .ToListAsync(ct);
        var returns = await db.Set<SupplierReturn>().AsNoTracking()
            .Where(x => x.Status == SupplierReturnStatus.Posted && x.PostedAtUtc >= fromUtc && x.PostedAtUtc < toUtc)
            .Select(x => new PurchasingPostedDocument(x.Id, PurchasingDocumentType.SupplierReturn, x.Number!, x.PostedAtUtc!.Value))
            .ToListAsync(ct);
        return [.. receipts.Concat(invoices).Concat(returns).OrderBy(x => x.PostedAtUtc).ThenBy(x => x.Id)];
    }

    public async Task<GoodsReceiptAccounting?> GetGoodsReceiptAsync(Guid receiptId, CancellationToken ct)
    {
        var receipt = await db.Set<GoodsReceipt>().AsNoTracking()
            .Where(x => x.Id == receiptId)
            .Select(x => new { x.Id, x.Number, x.SupplierId, x.BranchId, x.ReceivedAtUtc, x.StockDocumentId, x.PurchaseOrderId })
            .SingleOrDefaultAsync(ct);
        if (receipt is null)
            return null;
        var quantities = await db.Set<GoodsReceiptLine>().AsNoTracking()
            .Where(x => x.ReceiptId == receiptId)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var value = await ChangeAsync(receipt.PurchaseOrderId, quantities, receipt.ReceivedAtUtc, receipt.Id, ct);
        return new GoodsReceiptAccounting(
            receipt.Id, receipt.Number, receipt.SupplierId, receipt.BranchId, receipt.ReceivedAtUtc, receipt.StockDocumentId, value);
    }

    public async Task<SupplierInvoiceAccounting?> GetSupplierInvoiceAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Set<SupplierInvoice>().AsNoTracking()
            .Where(x => x.Id == invoiceId && x.Status == SupplierInvoiceStatus.Posted)
            .Select(x => new
            {
                x.Id, x.Number, x.SupplierInvoiceNumber, x.SupplierId, x.BranchId, x.InvoiceDate, x.DueDate, x.PostedAtUtc,
                x.NetAmount, x.TaxAmount, x.TotalAmount, x.PriceVarianceAmount, x.PurchaseOrderId,
            })
            .SingleOrDefaultAsync(ct);
        if (invoice is null)
            return null;
        var quantities = await db.Set<SupplierInvoiceLine>().AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(g => new { g.Key, Quantity = -g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var value = await ChangeAsync(invoice.PurchaseOrderId, quantities, invoice.PostedAtUtc!.Value, invoice.Id, ct);
        return new SupplierInvoiceAccounting(
            invoice.Id, invoice.Number!, invoice.SupplierInvoiceNumber, invoice.SupplierId, invoice.BranchId, invoice.InvoiceDate,
            invoice.DueDate, invoice.PostedAtUtc.Value, invoice.NetAmount, invoice.TaxAmount, invoice.TotalAmount, -value,
            invoice.PriceVarianceAmount);
    }

    public async Task<SupplierReturnAccounting?> GetSupplierReturnAsync(Guid returnId, CancellationToken ct)
    {
        var supplierReturn = await db.Set<SupplierReturn>().AsNoTracking()
            .Where(x => x.Id == returnId && x.Status == SupplierReturnStatus.Posted)
            .Select(x => new
            {
                x.Id, x.Number, x.SupplierId, x.BranchId, x.PostedAtUtc, x.StockDocumentId, x.PurchaseOrderId,
                x.CreditClaimNetAmount, x.CreditClaimTaxAmount, x.CreditClaimTotalAmount,
            })
            .SingleOrDefaultAsync(ct);
        if (supplierReturn is null)
            return null;
        var quantities = await db.Set<SupplierReturnLine>().AsNoTracking()
            .Where(x => x.ReturnId == returnId)
            .GroupBy(x => x.PurchaseOrderLineId)
            .Select(g => new { g.Key, Quantity = -g.Sum(x => x.UninvoicedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var value = await ChangeAsync(supplierReturn.PurchaseOrderId, quantities, supplierReturn.PostedAtUtc!.Value, supplierReturn.Id, ct);
        return new SupplierReturnAccounting(
            supplierReturn.Id, supplierReturn.Number!, supplierReturn.SupplierId, supplierReturn.BranchId,
            supplierReturn.PostedAtUtc.Value, supplierReturn.StockDocumentId!.Value, -value, supplierReturn.CreditClaimNetAmount,
            supplierReturn.CreditClaimTaxAmount, supplierReturn.CreditClaimTotalAmount);
    }

    public async Task<decimal> ReceivedNotInvoicedValueBeforeAsync(DateTime beforeUtc, CancellationToken ct)
    {
        var received = await (
                from line in db.Set<GoodsReceiptLine>()
                join receipt in db.Set<GoodsReceipt>() on line.ReceiptId equals receipt.Id
                where receipt.ReceivedAtUtc < beforeUtc
                group line by line.PurchaseOrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        var invoiced = await (
                from line in db.Set<SupplierInvoiceLine>()
                join invoice in db.Set<SupplierInvoice>() on line.InvoiceId equals invoice.Id
                where invoice.Status == SupplierInvoiceStatus.Posted && invoice.PostedAtUtc < beforeUtc
                group line by line.PurchaseOrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        var returned = await (
                from line in db.Set<SupplierReturnLine>()
                join supplierReturn in db.Set<SupplierReturn>() on line.ReturnId equals supplierReturn.Id
                where supplierReturn.Status == SupplierReturnStatus.Posted && supplierReturn.PostedAtUtc < beforeUtc
                group line by line.PurchaseOrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.UninvoicedQuantity) })
            .ToListAsync(ct);
        var uninvoiced = received.ToDictionary(x => x.Key, x => x.Quantity);
        foreach (var line in invoiced.Concat(returned))
            uninvoiced[line.Key] = uninvoiced.GetValueOrDefault(line.Key) - line.Quantity;
        var prices = await PricesAsync(uninvoiced.Where(x => x.Value != 0m).Select(x => x.Key).ToArray(), ct);
        return prices.Sum(p => UninvoicedReceipts.Value(uninvoiced[p.Key], p.Value.NetUnitPrice, p.Value.Decimals));
    }

    // The change a document made to its order lines' value: quantities are what it added to each line's uninvoiced quantity
    // (negative for invoices and returns).
    private async Task<decimal> ChangeAsync(
        Guid orderId, IReadOnlyDictionary<Guid, decimal> quantities, DateTime postedAtUtc, Guid documentId, CancellationToken ct)
    {
        if (quantities.Count == 0)
            return 0m;
        var lineIds = quantities.Keys.ToArray();
        var before = await UninvoicedBeforeAsync(lineIds, postedAtUtc, documentId, ct);
        var prices = await PricesAsync(lineIds, ct);
        var change = 0m;
        foreach (var (lineId, quantity) in quantities)
        {
            var price = prices.TryGetValue(lineId, out var known)
                ? known
                : throw new InvalidOperationException($"Order {orderId} has no line {lineId}.");
            change += UninvoicedReceipts.Change(before.GetValueOrDefault(lineId), quantity, price.NetUnitPrice, price.Decimals);
        }
        return change;
    }

    // Each line's uninvoiced quantity after the documents that posted before this one (timestamp, then id).
    private async Task<Dictionary<Guid, decimal>> UninvoicedBeforeAsync(
        Guid[] lineIds, DateTime postedAtUtc, Guid documentId, CancellationToken ct)
    {
        var received = await (
                from line in db.Set<GoodsReceiptLine>()
                join receipt in db.Set<GoodsReceipt>() on line.ReceiptId equals receipt.Id
                where lineIds.Contains(line.PurchaseOrderLineId) && receipt.ReceivedAtUtc <= postedAtUtc
                select new LineEvent(line.PurchaseOrderLineId, receipt.Id, receipt.ReceivedAtUtc, line.Quantity))
            .ToListAsync(ct);
        var invoiced = await (
                from line in db.Set<SupplierInvoiceLine>()
                join invoice in db.Set<SupplierInvoice>() on line.InvoiceId equals invoice.Id
                where lineIds.Contains(line.PurchaseOrderLineId) && invoice.Status == SupplierInvoiceStatus.Posted
                      && invoice.PostedAtUtc <= postedAtUtc
                select new LineEvent(line.PurchaseOrderLineId, invoice.Id, invoice.PostedAtUtc!.Value, -line.Quantity))
            .ToListAsync(ct);
        var returned = await (
                from line in db.Set<SupplierReturnLine>()
                join supplierReturn in db.Set<SupplierReturn>() on line.ReturnId equals supplierReturn.Id
                where lineIds.Contains(line.PurchaseOrderLineId) && supplierReturn.Status == SupplierReturnStatus.Posted
                      && supplierReturn.PostedAtUtc <= postedAtUtc
                select new LineEvent(line.PurchaseOrderLineId, supplierReturn.Id, supplierReturn.PostedAtUtc!.Value, -line.UninvoicedQuantity))
            .ToListAsync(ct);
        return received.Concat(invoiced).Concat(returned)
            .Where(e => e.PostedAtUtc < postedAtUtc || (e.PostedAtUtc == postedAtUtc && e.DocumentId.CompareTo(documentId) < 0))
            .GroupBy(e => e.LineId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Quantity));
    }

    private async Task<Dictionary<Guid, (decimal NetUnitPrice, int Decimals)>> PricesAsync(Guid[] lineIds, CancellationToken ct)
    {
        if (lineIds.Length == 0)
            return [];
        var rows = await (
                from line in db.Set<PurchaseOrderLine>()
                join order in db.Set<PurchaseOrder>() on line.OrderId equals order.Id
                where lineIds.Contains(line.Id)
                select new { line.Id, line.NetUnitPrice, order.CurrencyCode })
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.Id, x => (x.NetUnitPrice, Amounts.CurrencyDecimals(x.CurrencyCode)));
    }

    private sealed record LineEvent(Guid LineId, Guid DocumentId, DateTime PostedAtUtc, decimal Quantity);
}
