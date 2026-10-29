using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class ProductTaxDomainErrors
{
    public static readonly Error TreatmentInvalid = Error.BusinessRule(
        "Sales.ProductTax.TreatmentInvalid", "The tax treatment is standard, zero-rated or exempt.");

    public static readonly Error RateInvalid = Error.BusinessRule(
        "Sales.ProductTax.RateInvalid",
        "A standard-rated product's own rate is above 0 and at most 100 with at most 4 decimals; zero-rated and exempt products take none.");
}
