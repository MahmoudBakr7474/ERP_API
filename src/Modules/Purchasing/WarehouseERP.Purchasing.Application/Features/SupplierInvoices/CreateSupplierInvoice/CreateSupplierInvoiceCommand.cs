using WarehouseERP.Purchasing.Application.Contracts.SupplierInvoices;

namespace WarehouseERP.Purchasing.Application.Features.SupplierInvoices.CreateSupplierInvoice;

/// <param name="PurchaseOrderId">The one approved order the invoice bills; an order may have several invoices.</param>
/// <param name="SupplierInvoiceNumber">The supplier's number as printed; unique per supplier once normalized.</param>
/// <param name="InvoiceDate">Not after today in the company's time zone.</param>
/// <param name="DueDate">The invoice date plus the order's payment terms when left out.</param>
public sealed record CreateSupplierInvoiceCommand(
    Guid PurchaseOrderId, string SupplierInvoiceNumber, DateOnly InvoiceDate, DateOnly? DueDate, string? Notes,
    IReadOnlyList<InvoiceLineRequest> Lines);
