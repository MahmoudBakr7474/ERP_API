namespace WarehouseERP.Accounting.Contracts;

/// <summary>
/// What an account is used for when the system writes a journal by itself (year-end close now; ERP postings later). Each
/// role is mapped explicitly to one posting account; nothing in the code names an account, and an unmapped role stops
/// the posting that needs it instead of falling back to a default.
/// </summary>
public enum PostingRole : byte
{
    /// <summary>Where a closed year's profit or loss goes (equity).</summary>
    RetainedEarnings = 1,

    /// <summary>Where opening differences are placed deliberately by the accountant (equity).</summary>
    OpeningBalanceEquity = 2,

    /// <summary>The company's inventory at cost (asset).</summary>
    Inventory = 3,

    /// <summary>Goods received whose supplier invoice has not been posted yet (liability).</summary>
    GoodsReceivedNotInvoiced = 4,

    /// <summary>Supplier price differences on goods already sold or otherwise approved for expense (cost of sales).</summary>
    PurchasePriceVariance = 5,

    /// <summary>Supplier price differences waiting for the accountant's review (current asset or liability).</summary>
    PurchaseVarianceUnderReview = 6,

    /// <summary>Customers' control account (asset, customer lines).</summary>
    AccountsReceivable = 7,

    /// <summary>Revenue recognized on delivery before the invoice (asset).</summary>
    UnbilledReceivables = 8,

    /// <summary>Suppliers' control account (liability, supplier lines).</summary>
    AccountsPayable = 9,

    /// <summary>Recoverable VAT on purchases (asset).</summary>
    InputVat = 10,

    /// <summary>VAT charged on sales (liability).</summary>
    OutputVat = 11,

    /// <summary>Tax customers withheld from what they paid (asset).</summary>
    WithholdingTaxReceivable = 12,

    /// <summary>Tax the company withheld from what it paid suppliers, owed to the tax authority (liability).</summary>
    WithholdingTaxPayable = 13,

    /// <summary>Distribution sales (revenue).</summary>
    SalesRevenue = 14,

    /// <summary>Distribution sales returns (revenue, debit balance).</summary>
    SalesReturns = 15,

    /// <summary>Retail sales at the tills (revenue).</summary>
    PosRevenue = 16,

    /// <summary>Retail returns at the tills (revenue, debit balance).</summary>
    PosReturns = 17,

    /// <summary>Cost of goods sold (expense).</summary>
    CostOfSales = 18,

    /// <summary>Damaged, lost and expired stock (expense).</summary>
    InventoryLosses = 19,

    /// <summary>Stock found that was not recorded (revenue or a credit to expense).</summary>
    InventoryGains = 20,

    /// <summary>Corrections and other stock adjustments (expense).</summary>
    InventoryAdjustments = 21,

    /// <summary>Differences found by stock counts (expense).</summary>
    StockCountDifferences = 22,

    /// <summary>Manual inventory receipts without a supplier, waiting to be cleared (liability).</summary>
    UnmatchedReceipts = 23,

    /// <summary>Manual inventory issues for the company's own use (expense).</summary>
    InternalIssues = 24,

    /// <summary>Cash counted short or over at a till's close (expense).</summary>
    CashOverShort = 25,

    /// <summary>Card payments taken at tills, waiting for the acquirer's settlement (asset).</summary>
    CardClearing = 26,

    /// <summary>Cash taken at tills until the shift's cash is settled into a cashbox (asset).</summary>
    PosTillClearing = 27,

    /// <summary>Fees the acquirer keeps from card settlements (expense).</summary>
    CardFees = 28,
}
