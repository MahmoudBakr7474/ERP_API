namespace WarehouseERP.Purchasing.Contracts;

/// <summary>A kind of purchasing document; each has its own number series per branch and fiscal year.</summary>
public enum PurchasingDocumentType : byte
{
    /// <summary>A warehouse's request for goods (PR); whether it needs approval is a policy.</summary>
    PurchaseRequest = 1,

    /// <summary>An order to a supplier (PO); whether it needs approval is a policy.</summary>
    PurchaseOrder = 2,

    /// <summary>Goods received against an order (GRN); posts a purchase receipt to stock.</summary>
    GoodsReceipt = 3,

    /// <summary>A supplier invoice matched to an order's receipts (SIN); needs approval only for price differences.</summary>
    SupplierInvoice = 4,

    /// <summary>Received goods sent back to their supplier (SRN); always needs approval.</summary>
    SupplierReturn = 5,
}
