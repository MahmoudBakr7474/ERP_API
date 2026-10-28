using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Application rules about supplier invoices; the invoice's own invariants are SupplierInvoiceDomainErrors.
public static class SupplierInvoiceErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.SupplierInvoice.NotFound", "The supplier invoice does not exist.");

    public static readonly Error InvoiceNumberTaken = Error.Conflict(
        "Purchasing.SupplierInvoice.InvoiceNumberTaken", "This supplier already has an invoice with this number.");

    public static readonly Error InvoiceDateInFuture = Error.BusinessRule(
        "Purchasing.SupplierInvoice.InvoiceDateInFuture", "The invoice date can't be after today.");
}
