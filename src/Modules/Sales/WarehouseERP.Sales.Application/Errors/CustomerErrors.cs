using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about customers; the customer's own invariants are CustomerDomainErrors.
public static class CustomerErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Customer.NotFound", "The customer does not exist.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Sales.Customer.CodeTaken", "Another customer has this code.");

    public static readonly Error TaxNumberTaken = Error.Conflict(
        "Sales.Customer.TaxNumberTaken", "Another customer of this country has this tax registration number.");

    public static readonly Error CommercialRegistrationTaken = Error.Conflict(
        "Sales.Customer.CommercialRegistrationTaken",
        "Another customer has this commercial registration number at this registry office.");

    public static readonly Error NotActive = Error.BusinessRule(
        "Sales.Customer.NotActive", "The customer is inactive: it takes no new quotations or orders.");

    public static readonly Error OnCreditHold = Error.BusinessRule(
        "Sales.Customer.OnCreditHold", "The customer is on credit hold: release the hold before confirming or delivering.");

    public static readonly Error CountryNotFound = Error.BusinessRule(
        "Sales.Customer.CountryNotFound", "The country is not in the country list or is inactive.");

    public static readonly Error AddressNotFound = Error.BusinessRule(
        "Sales.Customer.AddressNotFound", "The delivery address is not one of the customer's addresses.");

    public static Error CityNotFound(int address) => Error.BusinessRule(
        "Sales.Customer.CityNotFound",
        $"Address {address}: the city is not in the lookups, or it, its governorate or its country is inactive.");
}
