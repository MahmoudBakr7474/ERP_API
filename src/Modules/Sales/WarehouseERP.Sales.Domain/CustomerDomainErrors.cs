using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class CustomerDomainErrors
{
    public static readonly Error TypeInvalid = Error.BusinessRule(
        "Sales.Customer.TypeInvalid", "A customer is an individual or a business.");

    public static readonly Error CountryInvalid = Error.BusinessRule(
        "Sales.Customer.CountryInvalid", "The country must be a two-letter ISO code.");

    public static readonly Error TaxNumberInvalid = Error.BusinessRule(
        "Sales.Customer.TaxNumberInvalid", "The tax registration number must be 1 to 30 letters and digits.");

    public static readonly Error EgyptianTaxNumberInvalid = Error.BusinessRule(
        "Sales.Customer.EgyptianTaxNumberInvalid", "An Egyptian tax registration number has 9 digits.");

    public static readonly Error CommercialRegistrationForBusinessOnly = Error.BusinessRule(
        "Sales.Customer.CommercialRegistrationForBusinessOnly", "Only a business customer has a commercial registration.");

    public static readonly Error CommercialRegistrationInvalid = Error.BusinessRule(
        "Sales.Customer.CommercialRegistrationInvalid", "The commercial registration number must be 1 to 30 letters and digits.");

    public static readonly Error CommercialRegistryRequired = Error.BusinessRule(
        "Sales.Customer.CommercialRegistryRequired", "Name the registry office that issued the commercial registration.");

    public static readonly Error PaymentTermsInvalid = Error.BusinessRule(
        "Sales.Customer.PaymentTermsInvalid", "Payment terms run from 0 to 365 days.");

    public static readonly Error CreditLimitInvalid = Error.BusinessRule(
        "Sales.Customer.CreditLimitInvalid", "A credit limit is 0 or more, with at most 4 decimals.");

    public static readonly Error CreditHoldReasonRequired = Error.BusinessRule(
        "Sales.Customer.CreditHoldReasonRequired", "Say why the customer is put on credit hold.");

    public static readonly Error TooManyContacts = Error.BusinessRule(
        "Sales.Customer.TooManyContacts", "A customer has at most 20 contacts.");

    public static readonly Error TooManyAddresses = Error.BusinessRule(
        "Sales.Customer.TooManyAddresses", "A customer has at most 10 addresses.");

    public static readonly Error SeveralPrimaryContacts = Error.BusinessRule(
        "Sales.Customer.SeveralPrimaryContacts", "Only one contact can be the primary one.");

    public static readonly Error SeveralPrimaryAddresses = Error.BusinessRule(
        "Sales.Customer.SeveralPrimaryAddresses", "Only one address can be the primary one.");
}
