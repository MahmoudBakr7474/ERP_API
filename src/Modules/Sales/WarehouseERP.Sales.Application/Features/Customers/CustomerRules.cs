using FluentValidation;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Application.Features.Customers;

// The shape of a customer's contacts and addresses, shared by the create and update validators. Registration numbers
// are checked by the domain once normalized.
internal static class CustomerRules
{
    public const string CountryPattern = "^[A-Za-z]{2}$";

    public static void Contact(InlineValidator<CustomerContactRequest> contact)
    {
        contact.RuleLevelCascadeMode = CascadeMode.Stop;
        contact.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        contact.RuleFor(x => x.JobTitle).MaximumLength(100);
        contact.RuleFor(x => x.Phone).MaximumLength(30);
        contact.RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }

    public static void Address(InlineValidator<CustomerAddressRequest> address)
    {
        address.RuleLevelCascadeMode = CascadeMode.Stop;
        address.RuleFor(x => x.AddressType).IsInEnum();
        address.RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        address.RuleFor(x => x.Line2).MaximumLength(200);
        address.RuleFor(x => x.CityId).NotEmpty();
        address.RuleFor(x => x.PostalCode).MaximumLength(20);
    }

    public static IReadOnlyList<CustomerContactInput> Contacts(IReadOnlyList<CustomerContactRequest>? contacts) =>
        (contacts ?? []).Select(c => new CustomerContactInput(c.Name, c.JobTitle, c.Phone, c.Email, c.IsPrimary)).ToList();

    public static IReadOnlyList<CustomerAddressInput> Addresses(IReadOnlyList<CustomerAddressRequest>? addresses) =>
        (addresses ?? []).Select(a => new CustomerAddressInput(
            a.AddressType, a.Line1, a.Line2, a.CityId, a.PostalCode, a.IsPrimary)).ToList();
}
