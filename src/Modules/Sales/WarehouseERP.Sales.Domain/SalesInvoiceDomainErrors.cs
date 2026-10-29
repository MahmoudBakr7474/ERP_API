using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesInvoiceDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Sales.Invoice.NotDraft", "Only a draft invoice can be changed, posted or cancelled; posted invoices never change.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Sales.Invoice.NoLines", "An invoice needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Sales.Invoice.TooManyLines", "An invoice holds at most 200 lines.");
}
