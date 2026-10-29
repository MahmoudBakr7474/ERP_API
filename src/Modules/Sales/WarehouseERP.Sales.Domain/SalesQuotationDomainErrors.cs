using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesQuotationDomainErrors
{
    public static readonly Error NotDraft = Error.BusinessRule(
        "Sales.Quotation.NotDraft", "Only a draft quotation can be changed or submitted.");

    public static readonly Error NotSubmitted = Error.BusinessRule(
        "Sales.Quotation.NotSubmitted", "Only a submitted quotation can be approved or rejected.");

    public static readonly Error NotIssued = Error.BusinessRule(
        "Sales.Quotation.NotIssued", "Only an issued quotation can be declined or converted into an order.");

    public static readonly Error NotCancellable = Error.BusinessRule(
        "Sales.Quotation.NotCancellable", "A converted, declined or cancelled quotation can't be cancelled.");

    public static readonly Error Expired = Error.BusinessRule(
        "Sales.Quotation.Expired", "The quotation is past its validity date; issue a new one.");

    public static readonly Error NoLines = Error.BusinessRule(
        "Sales.Quotation.NoLines", "A quotation needs at least one line.");

    public static readonly Error TooManyLines = Error.BusinessRule(
        "Sales.Quotation.TooManyLines", "A quotation holds at most 200 lines.");

    public static readonly Error ValidityInvalid = Error.BusinessRule(
        "Sales.Quotation.ValidityInvalid", "A quotation can't expire before its date.");

    public static readonly Error PaymentTermsInvalid = Error.BusinessRule(
        "Sales.Quotation.PaymentTermsInvalid", "Payment terms run from 0 to 365 days.");
}
