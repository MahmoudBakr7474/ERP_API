namespace WarehouseERP.Accounting.Contracts;

/// <summary>An operational document Accounting journals by itself once accounting is active (ADR 0020, Milestone C).</summary>
public enum SourceDocumentType : byte
{
    /// <summary>An Inventory stock document of its own: opening stock, receipt or issue, adjustment, reversal, count.</summary>
    InventoryDocument = 1,

    /// <summary>A Purchasing goods receipt: stock in against goods received not invoiced.</summary>
    GoodsReceipt = 2,

    /// <summary>A posted supplier invoice: the payable, recoverable VAT and price differences.</summary>
    SupplierInvoice = 3,

    /// <summary>A posted supplier return: stock out and, for invoiced goods, the claim on the supplier.</summary>
    SupplierReturn = 4,

    /// <summary>A posted sales delivery: revenue against unbilled receivables, and the cost of the goods.</summary>
    SalesDelivery = 5,

    /// <summary>A posted sales invoice: the receivable and output VAT, unbilled receivables cleared.</summary>
    SalesInvoice = 6,

    /// <summary>An approved customer return: the customer's credit for invoiced goods, and the goods back in stock.</summary>
    SalesReturn = 7,

    /// <summary>A POS sale: retail revenue and VAT against the till's cash and card clearing, and the cost of the goods.</summary>
    PosSale = 8,

    /// <summary>A POS return: the refund out of the till's cash or card clearing, and restocked goods.</summary>
    PosReturn = 9,

    /// <summary>A closed POS shift: its cash handed in to the branch's cashbox, with any difference counted.</summary>
    PosShift = 10,
}
