using Microsoft.EntityFrameworkCore;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.ReadModels;

// What Accounting may learn about posted sales documents, read without locks. Unbilled goods are valued per order line
// (UnbilledDeliveries): a document's amount is the change it made to its lines' value, from the uninvoiced quantity left
// by every earlier delivery, invoice and return of those lines (in posting order) to what it left. Posting order is the
// documents' own timestamps (a return's approval), then their ids; each of them changed the order in its transaction, so
// they can't interleave.
internal sealed class SalesAccountingSource(DbContext db) : ISalesAccountingSource
{
    public async Task<IReadOnlyList<SalesPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var deliveries = await db.Set<SalesDelivery>().AsNoTracking()
            .Where(x => x.Status == SalesDeliveryStatus.Posted && x.PostedAtUtc >= fromUtc && x.PostedAtUtc < toUtc)
            .Select(x => new SalesPostedDocument(x.Id, SalesDocumentType.Delivery, x.Number!, x.PostedAtUtc!.Value))
            .ToListAsync(ct);
        var invoices = await db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => x.Status == SalesInvoiceStatus.Posted && x.PostedAtUtc >= fromUtc && x.PostedAtUtc < toUtc)
            .Select(x => new SalesPostedDocument(x.Id, SalesDocumentType.Invoice, x.Number!, x.PostedAtUtc!.Value))
            .ToListAsync(ct);
        var returns = await db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Status == SalesReturnStatus.Approved && x.ApprovedAtUtc >= fromUtc && x.ApprovedAtUtc < toUtc)
            .Select(x => new SalesPostedDocument(x.Id, SalesDocumentType.Return, x.Number!, x.ApprovedAtUtc!.Value))
            .ToListAsync(ct);
        return [.. deliveries.Concat(invoices).Concat(returns).OrderBy(x => x.PostedAtUtc).ThenBy(x => x.Id)];
    }

    public async Task<SalesDeliveryAccounting?> GetDeliveryAsync(Guid deliveryId, CancellationToken ct)
    {
        var delivery = await db.Set<SalesDelivery>().AsNoTracking()
            .Where(x => x.Id == deliveryId && x.Status == SalesDeliveryStatus.Posted)
            .Select(x => new { x.Id, x.Number, x.CustomerId, x.BranchId, x.PostedAtUtc, x.StockDocumentId })
            .SingleOrDefaultAsync(ct);
        if (delivery is null)
            return null;
        var quantities = await db.Set<SalesDeliveryLine>().AsNoTracking()
            .Where(x => x.DeliveryId == deliveryId)
            .GroupBy(x => x.OrderLineId)
            .Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var revenue = await ChangeAsync(quantities, delivery.PostedAtUtc!.Value, delivery.Id, ct);
        return new SalesDeliveryAccounting(
            delivery.Id, delivery.Number!, delivery.CustomerId, delivery.BranchId, delivery.PostedAtUtc.Value,
            delivery.StockDocumentId!.Value, revenue);
    }

    public async Task<SalesInvoiceAccounting?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => x.Id == invoiceId && x.Status == SalesInvoiceStatus.Posted)
            .Select(x => new
            {
                x.Id, x.Number, x.CustomerId, x.BranchId, x.InvoiceDate, x.DueDate, x.PostedAtUtc, x.NetAmount, x.TaxAmount,
                x.TotalAmount,
            })
            .SingleOrDefaultAsync(ct);
        if (invoice is null)
            return null;
        var quantities = await db.Set<SalesInvoiceLine>().AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .GroupBy(x => x.OrderLineId)
            .Select(g => new { g.Key, Quantity = -g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var change = await ChangeAsync(quantities, invoice.PostedAtUtc!.Value, invoice.Id, ct);
        return new SalesInvoiceAccounting(
            invoice.Id, invoice.Number!, invoice.CustomerId, invoice.BranchId, invoice.InvoiceDate, invoice.DueDate,
            invoice.PostedAtUtc.Value, invoice.NetAmount, invoice.TaxAmount, invoice.TotalAmount, -change);
    }

    public async Task<SalesReturnAccounting?> GetReturnAsync(Guid returnId, CancellationToken ct)
    {
        var salesReturn = await db.Set<SalesReturn>().AsNoTracking()
            .Where(x => x.Id == returnId && x.Status == SalesReturnStatus.Approved)
            .Select(x => new
            {
                x.Id, x.Number, x.CustomerId, x.BranchId, x.ApprovedAtUtc, x.StockDocumentId, x.CreditNetAmount, x.CreditTaxAmount,
                x.CreditTotalAmount,
            })
            .SingleOrDefaultAsync(ct);
        if (salesReturn is null)
            return null;
        var quantities = await db.Set<SalesReturnLine>().AsNoTracking()
            .Where(x => x.ReturnId == returnId)
            .GroupBy(x => x.OrderLineId)
            .Select(g => new { g.Key, Quantity = -g.Sum(x => x.UninvoicedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var change = await ChangeAsync(quantities, salesReturn.ApprovedAtUtc!.Value, salesReturn.Id, ct);
        return new SalesReturnAccounting(
            salesReturn.Id, salesReturn.Number!, salesReturn.CustomerId, salesReturn.BranchId, salesReturn.ApprovedAtUtc.Value,
            salesReturn.StockDocumentId!.Value, -change, salesReturn.CreditNetAmount, salesReturn.CreditTaxAmount,
            salesReturn.CreditTotalAmount);
    }

    public async Task<decimal> UnbilledValueBeforeAsync(DateTime beforeUtc, CancellationToken ct)
    {
        var delivered = await (
                from line in db.Set<SalesDeliveryLine>()
                join delivery in db.Set<SalesDelivery>() on line.DeliveryId equals delivery.Id
                where delivery.Status == SalesDeliveryStatus.Posted && delivery.PostedAtUtc < beforeUtc
                group line by line.OrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        var invoiced = await (
                from line in db.Set<SalesInvoiceLine>()
                join invoice in db.Set<SalesInvoice>() on line.InvoiceId equals invoice.Id
                where invoice.Status == SalesInvoiceStatus.Posted && invoice.PostedAtUtc < beforeUtc
                group line by line.OrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);
        var returned = await (
                from line in db.Set<SalesReturnLine>()
                join salesReturn in db.Set<SalesReturn>() on line.ReturnId equals salesReturn.Id
                where salesReturn.Status == SalesReturnStatus.Approved && salesReturn.ApprovedAtUtc < beforeUtc
                group line by line.OrderLineId into g
                select new { g.Key, Quantity = g.Sum(x => x.UninvoicedQuantity) })
            .ToListAsync(ct);
        var uninvoiced = delivered.ToDictionary(x => x.Key, x => x.Quantity);
        foreach (var line in invoiced.Concat(returned))
            uninvoiced[line.Key] = uninvoiced.GetValueOrDefault(line.Key) - line.Quantity;
        var prices = await PricesAsync(uninvoiced.Where(x => x.Value != 0m).Select(x => x.Key).ToArray(), ct);
        return prices.Sum(p => UnbilledDeliveries.Value(uninvoiced[p.Key], p.Value.Price, p.Value.Decimals));
    }

    // The change a document made to its order lines' unbilled value: quantities are what it added to each line's uninvoiced
    // quantity (negative for invoices and returns).
    private async Task<decimal> ChangeAsync(IReadOnlyDictionary<Guid, decimal> quantities, DateTime postedAtUtc, Guid documentId, CancellationToken ct)
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
                : throw new InvalidOperationException($"Order line {lineId} does not exist.");
            change += UnbilledDeliveries.Change(before.GetValueOrDefault(lineId), quantity, price.Price, price.Decimals);
        }
        return change;
    }

    // Each line's uninvoiced quantity after the documents that posted before this one (timestamp, then id).
    private async Task<Dictionary<Guid, decimal>> UninvoicedBeforeAsync(
        Guid[] lineIds, DateTime postedAtUtc, Guid documentId, CancellationToken ct)
    {
        var delivered = await (
                from line in db.Set<SalesDeliveryLine>()
                join delivery in db.Set<SalesDelivery>() on line.DeliveryId equals delivery.Id
                where lineIds.Contains(line.OrderLineId) && delivery.Status == SalesDeliveryStatus.Posted
                      && delivery.PostedAtUtc <= postedAtUtc
                select new LineEvent(line.OrderLineId, delivery.Id, delivery.PostedAtUtc!.Value, line.Quantity))
            .ToListAsync(ct);
        var invoiced = await (
                from line in db.Set<SalesInvoiceLine>()
                join invoice in db.Set<SalesInvoice>() on line.InvoiceId equals invoice.Id
                where lineIds.Contains(line.OrderLineId) && invoice.Status == SalesInvoiceStatus.Posted
                      && invoice.PostedAtUtc <= postedAtUtc
                select new LineEvent(line.OrderLineId, invoice.Id, invoice.PostedAtUtc!.Value, -line.Quantity))
            .ToListAsync(ct);
        var returned = await (
                from line in db.Set<SalesReturnLine>()
                join salesReturn in db.Set<SalesReturn>() on line.ReturnId equals salesReturn.Id
                where lineIds.Contains(line.OrderLineId) && salesReturn.Status == SalesReturnStatus.Approved
                      && salesReturn.ApprovedAtUtc <= postedAtUtc
                select new LineEvent(line.OrderLineId, salesReturn.Id, salesReturn.ApprovedAtUtc!.Value, -line.UninvoicedQuantity))
            .ToListAsync(ct);
        return delivered.Concat(invoiced).Concat(returned)
            .Where(e => e.PostedAtUtc < postedAtUtc || (e.PostedAtUtc == postedAtUtc && e.DocumentId.CompareTo(documentId) < 0))
            .GroupBy(e => e.LineId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Quantity));
    }

    private async Task<Dictionary<Guid, (LinePrice Price, int Decimals)>> PricesAsync(Guid[] lineIds, CancellationToken ct)
    {
        if (lineIds.Length == 0)
            return [];
        var rows = await (
                from line in db.Set<SalesOrderLine>()
                join order in db.Set<SalesOrder>() on line.OrderId equals order.Id
                where lineIds.Contains(line.Id)
                select new { line.Id, line.UnitPrice, line.DiscountPercent, line.TaxTreatment, line.TaxRate, order.CurrencyCode })
            .ToListAsync(ct);
        return rows.ToDictionary(
            x => x.Id,
            x => (new LinePrice(x.UnitPrice, x.DiscountPercent, x.TaxTreatment, x.TaxRate), Amounts.CurrencyDecimals(x.CurrencyCode)));
    }

    private sealed record LineEvent(Guid LineId, Guid DocumentId, DateTime PostedAtUtc, decimal Quantity);
}
