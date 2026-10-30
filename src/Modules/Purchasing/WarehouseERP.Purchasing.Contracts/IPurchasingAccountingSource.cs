namespace WarehouseERP.Purchasing.Contracts;

/// <summary>
/// Read-only view of posted purchasing documents for Accounting (ADR 0020, Milestone C): goods receipts, supplier invoices
/// and supplier returns with the amounts Purchasing owns, and the value of goods received but not invoiced at a moment.
/// Accounting journals them later and never changes anything here, so no purchasing workflow waits on Accounting.
/// </summary>
public interface IPurchasingAccountingSource
{
    /// <summary>
    /// Goods receipts, posted supplier invoices and posted supplier returns of the period (at or after
    /// <paramref name="fromUtc"/>, before <paramref name="toUtc"/>), oldest first.
    /// </summary>
    Task<IReadOnlyList<PurchasingPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    /// <summary>A goods receipt, or null when there is none.</summary>
    Task<GoodsReceiptAccounting?> GetGoodsReceiptAsync(Guid receiptId, CancellationToken ct);

    /// <summary>A posted supplier invoice, or null when there is none.</summary>
    Task<SupplierInvoiceAccounting?> GetSupplierInvoiceAsync(Guid invoiceId, CancellationToken ct);

    /// <summary>A posted supplier return, or null when there is none.</summary>
    Task<SupplierReturnAccounting?> GetSupplierReturnAsync(Guid returnId, CancellationToken ct);

    /// <summary>
    /// The value of goods received and not invoiced just before the moment: per order line, what was received less what
    /// invoices billed and what went back before invoicing, at the order's net unit price.
    /// </summary>
    Task<decimal> ReceivedNotInvoicedValueBeforeAsync(DateTime beforeUtc, CancellationToken ct);
}
