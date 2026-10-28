using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

/// <summary>One line of a supplier invoice as the client sends it: the order line it bills and the supplier's price.</summary>
/// <param name="PurchaseOrderLineId">A line of the invoice's order; each at most once.</param>
/// <param name="Quantity">In the order line's unit; at most what was received and is not yet invoiced.</param>
/// <param name="UnitPrice">The supplier's price before discount and tax; a net price unlike the order's needs approval.</param>
/// <param name="TaxTreatment">Only Recoverable is supported; NonRecoverable is refused.</param>
public sealed record InvoiceLineRequest(
    Guid PurchaseOrderLineId, decimal Quantity, decimal UnitPrice, decimal DiscountPercent = 0, decimal TaxRate = 0,
    TaxTreatment TaxTreatment = TaxTreatment.Recoverable);
