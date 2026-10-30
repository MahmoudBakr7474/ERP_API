namespace WarehouseERP.Sales.Contracts;

/// <summary>
/// Read-only view of posted sales documents for Accounting (ADR 0020, Milestone C): deliveries (revenue is recognized when
/// goods are delivered), invoices and approved customer returns with the amounts Sales owns, and the value of goods
/// delivered but not invoiced at a moment. Accounting journals them later and never changes anything here, so no sales
/// workflow waits on Accounting.
/// </summary>
public interface ISalesAccountingSource
{
    /// <summary>
    /// Posted deliveries and invoices and approved returns of the period (at or after <paramref name="fromUtc"/>, before
    /// <paramref name="toUtc"/>), oldest first.
    /// </summary>
    Task<IReadOnlyList<SalesPostedDocument>> ListPostedAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    /// <summary>A posted delivery, or null when there is none.</summary>
    Task<SalesDeliveryAccounting?> GetDeliveryAsync(Guid deliveryId, CancellationToken ct);

    /// <summary>A posted invoice, or null when there is none.</summary>
    Task<SalesInvoiceAccounting?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct);

    /// <summary>An approved customer return, or null when there is none.</summary>
    Task<SalesReturnAccounting?> GetReturnAsync(Guid returnId, CancellationToken ct);

    /// <summary>
    /// The value of goods delivered and not invoiced just before the moment: per order line, what was delivered less what
    /// invoices billed and what came back before invoicing, at the line's agreed price and discount (net of VAT).
    /// </summary>
    Task<decimal> UnbilledValueBeforeAsync(DateTime beforeUtc, CancellationToken ct);
}
