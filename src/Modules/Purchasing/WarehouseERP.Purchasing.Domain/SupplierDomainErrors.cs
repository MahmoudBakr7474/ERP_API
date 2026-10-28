using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

public static class SupplierDomainErrors
{
    public static readonly Error CountryInvalid = Error.BusinessRule(
        "Purchasing.Supplier.CountryInvalid", "The country must be a two-letter ISO code.");

    public static readonly Error TaxNumberInvalid = Error.BusinessRule(
        "Purchasing.Supplier.TaxNumberInvalid", "The tax registration number must be 1 to 30 letters and digits.");

    public static readonly Error EgyptianTaxNumberInvalid = Error.BusinessRule(
        "Purchasing.Supplier.EgyptianTaxNumberInvalid", "An Egyptian tax registration number has 9 digits.");

    public static readonly Error CommercialRegistrationInvalid = Error.BusinessRule(
        "Purchasing.Supplier.CommercialRegistrationInvalid", "The commercial registration number must be 1 to 30 letters and digits.");

    public static readonly Error CommercialRegistryRequired = Error.BusinessRule(
        "Purchasing.Supplier.CommercialRegistryRequired", "Name the registry office that issued the commercial registration.");

    public static readonly Error PaymentTermsInvalid = Error.BusinessRule(
        "Purchasing.Supplier.PaymentTermsInvalid", "Payment terms run from 0 to 365 days.");

    public static readonly Error TooManyContacts = Error.BusinessRule(
        "Purchasing.Supplier.TooManyContacts", "A supplier has at most 20 contacts.");

    public static readonly Error TooManyAddresses = Error.BusinessRule(
        "Purchasing.Supplier.TooManyAddresses", "A supplier has at most 10 addresses.");

    public static readonly Error SeveralPrimaryContacts = Error.BusinessRule(
        "Purchasing.Supplier.SeveralPrimaryContacts", "Only one contact can be the primary one.");

    public static readonly Error SeveralPrimaryAddresses = Error.BusinessRule(
        "Purchasing.Supplier.SeveralPrimaryAddresses", "Only one address can be the primary one.");
}
