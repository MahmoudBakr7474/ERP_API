using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about sales invoices; the invoice's own invariants are SalesInvoiceDomainErrors.
public static class InvoiceErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Invoice.NotFound", "The invoice does not exist.");

    public static readonly Error DateInFuture = Error.BusinessRule(
        "Sales.Invoice.DateInFuture", "An invoice can't be dated after today.");
}
