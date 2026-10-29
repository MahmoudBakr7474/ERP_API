using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about quotations; the quotation's own invariants are SalesQuotationDomainErrors.
public static class QuotationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Quotation.NotFound", "The quotation does not exist.");
}
