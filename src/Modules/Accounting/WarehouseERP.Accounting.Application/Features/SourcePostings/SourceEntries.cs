using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

/// <summary>
/// The posting rules of operational documents (V1 decisions 8; ADR 0020, Milestone C), as pure functions of what the
/// modules report. Stock values come from Inventory's cost entries rounded once per document to the currency's minor unit;
/// every other amount is the module's own.
/// <list type="bullet">
/// <item>Inventory's own documents: stock against the account of their kind (opening stock against opening-balance equity,
/// receipts without a supplier against unmatched receipts, issues against internal use, adjustments by reason, counts
/// against count differences); a reversal uses the reversed document's account the other way.</item>
/// <item>Goods receipt: inventory against goods received not invoiced, at the order's value of the goods (which also
/// clears exactly when they are invoiced or returned).</item>
/// <item>Supplier invoice: goods received not invoiced cleared at that value, input VAT, the supplier's payable; the
/// difference from the invoice's net is a price difference kept under review: Inventory does not revalue posted stock,
/// so splitting it between stock held and stock sold is the accountant's call (V1 decision 8).</item>
/// <item>Supplier return: stock out at Inventory's value; goods returned before invoicing clear goods received not
/// invoiced, invoiced goods become a claim on the supplier (a debit on the payable, with its VAT); the remaining
/// difference is kept under review.</item>
/// <item>Sales delivery: revenue against unbilled receivables when goods are delivered (transfer of control), and the cost
/// of the goods.</item>
/// <item>Sales invoice: the receivable and output VAT, unbilled receivables cleared without recognizing revenue again (only
/// a rounding difference between the invoice and the delivered value goes to revenue).</item>
/// <item>Customer return: a sales return with its VAT as a credit on the receivable for invoiced goods, unbilled receivables
/// cleared for goods returned before invoicing, and the goods back at their cost.</item>
/// <item>POS sale and return: retail revenue or returns and VAT against the till's cash clearing and card clearing, and
/// the goods' cost; a closed shift hands its takings in to the branch's POS cashbox, the counted difference to cash over
/// and short. Card clearing waits for the acquirer's settlement, recorded by the treasury.</item>
/// </list>
/// </summary>
public static class SourceEntries
{
    public static Result<SourceEntry> InventoryDocument(InventoryDocumentValue document, int decimals)
    {
        var reversal = document.DocumentType == StockDocumentType.Reversal;
        var kind = reversal ? document.ReversedDocumentType : document.DocumentType;
        PostingRole? counter = kind switch
        {
            StockDocumentType.OpeningBalance => PostingRole.OpeningBalanceEquity,
            StockDocumentType.Receipt => PostingRole.UnmatchedReceipts,
            StockDocumentType.Issue => PostingRole.InternalIssues,
            StockDocumentType.Adjustment => document.AdjustmentReason switch
            {
                AdjustmentReason.Found => PostingRole.InventoryGains,
                AdjustmentReason.Damage or AdjustmentReason.Loss or AdjustmentReason.Expiry => PostingRole.InventoryLosses,
                _ => PostingRole.InventoryAdjustments,
            },
            StockDocumentType.CountAdjustment => PostingRole.StockCountDifferences,
            _ => null,
        };
        if (counter is not { } role)
            return SourcePostingErrors.UnsupportedDocument(document.Number);
        var valueIn = Round(document.ValueIn, decimals);
        var valueOut = Round(document.ValueOut, decimals);
        var description = reversal ? $"عكس المستند المخزني {document.ReversedNumber}" : $"مستند مخزني {document.Number}";
        return new SourceEntry(JournalType.InventoryJournal, document.BranchId, description, document.Number,
            [Line(PostingRole.Inventory, valueIn), Line(role, -valueIn), Line(role, valueOut), Line(PostingRole.Inventory, -valueOut)],
            document.ValueIn - document.ValueOut, valueIn - valueOut, null);
    }

    public static SourceEntry GoodsReceipt(GoodsReceiptAccounting receipt, decimal stockValue) =>
        new(JournalType.PurchasingJournal, receipt.BranchId, $"استلام بضاعة {receipt.Number}", receipt.Number,
            [Line(PostingRole.Inventory, receipt.ReceivedValue), Line(PostingRole.GoodsReceivedNotInvoiced, -receipt.ReceivedValue)],
            stockValue, receipt.ReceivedValue, null);

    public static SourceEntry SupplierInvoice(SupplierInvoiceAccounting invoice)
    {
        var variance = invoice.NetAmount - invoice.ReceivedValue;
        return new SourceEntry(JournalType.PurchasingJournal, invoice.BranchId,
            $"فاتورة مورد {invoice.Number} ({invoice.SupplierInvoiceNumber})", invoice.Number,
            [
                Line(PostingRole.GoodsReceivedNotInvoiced, invoice.ReceivedValue),
                Line(PostingRole.PurchaseVarianceUnderReview, variance),
                Line(PostingRole.InputVat, invoice.TaxAmount),
                Supplier(-invoice.TotalAmount, invoice.SupplierId, invoice.DueDate),
            ],
            null, null, variance == 0m ? null : variance);
    }

    public static SourceEntry SupplierReturn(SupplierReturnAccounting supplierReturn, decimal stockValue, int decimals)
    {
        var cost = Round(-stockValue, decimals);
        var variance = cost - supplierReturn.ReceivedValue - supplierReturn.ClaimNetAmount;
        return new SourceEntry(JournalType.PurchasingJournal, supplierReturn.BranchId, $"مرتجع مشتريات {supplierReturn.Number}",
            supplierReturn.Number,
            [
                Supplier(supplierReturn.ClaimTotalAmount, supplierReturn.SupplierId, null),
                Line(PostingRole.GoodsReceivedNotInvoiced, supplierReturn.ReceivedValue),
                Line(PostingRole.PurchaseVarianceUnderReview, variance),
                Line(PostingRole.InputVat, -supplierReturn.ClaimTaxAmount),
                Line(PostingRole.Inventory, -cost),
            ],
            stockValue, -cost, variance == 0m ? null : variance);
    }

    public static SourceEntry SalesDelivery(SalesDeliveryAccounting delivery, decimal stockValue, int decimals)
    {
        var cost = Round(-stockValue, decimals);
        return new SourceEntry(JournalType.SalesJournal, delivery.BranchId, $"تسليم مبيعات {delivery.Number}", delivery.Number,
            [
                Line(PostingRole.UnbilledReceivables, delivery.RevenueAmount), Line(PostingRole.SalesRevenue, -delivery.RevenueAmount),
                Line(PostingRole.CostOfSales, cost), Line(PostingRole.Inventory, -cost),
            ],
            stockValue, -cost, null);
    }

    public static SourceEntry SalesInvoice(SalesInvoiceAccounting invoice) =>
        new(JournalType.SalesJournal, invoice.BranchId, $"فاتورة مبيعات {invoice.Number}", invoice.Number,
            [
                Customer(invoice.TotalAmount, invoice.CustomerId, invoice.DueDate),
                Line(PostingRole.UnbilledReceivables, -invoice.UnbilledAmount),
                Line(PostingRole.SalesRevenue, invoice.UnbilledAmount - invoice.NetAmount),
                Line(PostingRole.OutputVat, -invoice.TaxAmount),
            ],
            null, null, null);

    public static SourceEntry SalesReturn(SalesReturnAccounting salesReturn, decimal stockValue, int decimals)
    {
        var cost = Round(stockValue, decimals);
        return new SourceEntry(JournalType.SalesJournal, salesReturn.BranchId, $"مرتجع مبيعات {salesReturn.Number}", salesReturn.Number,
            [
                Line(PostingRole.SalesReturns, salesReturn.CreditNetAmount + salesReturn.UnbilledAmount),
                Line(PostingRole.OutputVat, salesReturn.CreditTaxAmount),
                Customer(-salesReturn.CreditTotalAmount, salesReturn.CustomerId, null),
                Line(PostingRole.UnbilledReceivables, -salesReturn.UnbilledAmount),
                Line(PostingRole.Inventory, cost), Line(PostingRole.CostOfSales, -cost),
            ],
            stockValue, cost, null);
    }

    public static SourceEntry PosSale(PosSaleAccounting sale, decimal? stockValue, int decimals)
    {
        var cost = Round(-(stockValue ?? 0m), decimals);
        return new SourceEntry(JournalType.PosJournal, sale.BranchId, $"بيع نقاط البيع {sale.Number}", sale.Number,
            [
                Line(PostingRole.PosTillClearing, sale.CashAmount), Line(PostingRole.CardClearing, sale.CardAmount),
                Line(PostingRole.PosRevenue, -sale.NetAmount), Line(PostingRole.OutputVat, -sale.TaxAmount),
                Line(PostingRole.CostOfSales, cost), Line(PostingRole.Inventory, -cost),
            ],
            stockValue, stockValue is null ? null : -cost, null);
    }

    public static SourceEntry PosReturn(PosReturnAccounting posReturn, decimal? stockValue, int decimals)
    {
        var cost = Round(stockValue ?? 0m, decimals);
        return new SourceEntry(JournalType.PosJournal, posReturn.BranchId, $"مرتجع نقاط البيع {posReturn.Number} من {posReturn.SaleNumber}",
            posReturn.Number,
            [
                Line(PostingRole.PosReturns, posReturn.NetAmount), Line(PostingRole.OutputVat, posReturn.TaxAmount),
                Line(PostingRole.PosTillClearing, -posReturn.CashRefundAmount), Line(PostingRole.CardClearing, -posReturn.CardRefundAmount),
                Line(PostingRole.Inventory, cost), Line(PostingRole.CostOfSales, -cost),
            ],
            stockValue, stockValue is null ? null : cost, null);
    }

    /// <summary>A closed shift: its takings and the counted difference into the cashbox, the takings off the till's clearing.</summary>
    public static SourceEntry PosShift(PosShiftAccounting shift, Guid cashboxAccountId) =>
        new(JournalType.PosJournal, shift.BranchId, $"إقفال وردية {shift.Number}", shift.Number,
            [
                new SourceLine(null, cashboxAccountId, shift.CashTakings + shift.CashDifference, null, null, null),
                Line(PostingRole.PosTillClearing, -shift.CashTakings),
                Line(PostingRole.CashOverShort, -shift.CashDifference),
            ],
            null, null, null);

    private static SourceLine Line(PostingRole role, decimal amount) => new(role, null, amount, null, null, null);

    private static SourceLine Customer(decimal amount, Guid customerId, DateOnly? dueDate) =>
        new(PostingRole.AccountsReceivable, null, amount, PartyType.Customer, customerId, dueDate);

    private static SourceLine Supplier(decimal amount, Guid supplierId, DateOnly? dueDate) =>
        new(PostingRole.AccountsPayable, null, amount, PartyType.Supplier, supplierId, dueDate);

    private static decimal Round(decimal value, int decimals) => decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
}
