using FluentValidation;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Domain;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers;

// The shape of a supplier's contacts and addresses, shared by the create and update validators. Registration numbers
// are checked by the domain once normalized.
internal static class SupplierRules
{
    public const string CountryPattern = "^[A-Za-z]{2}$";

    public static void Contact(InlineValidator<SupplierContactRequest> contact)
    {
        contact.RuleLevelCascadeMode = CascadeMode.Stop;
        contact.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        contact.RuleFor(x => x.JobTitle).MaximumLength(100);
        contact.RuleFor(x => x.Phone).MaximumLength(30);
        contact.RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }

    public static void Address(InlineValidator<SupplierAddressRequest> address)
    {
        address.RuleLevelCascadeMode = CascadeMode.Stop;
        address.RuleFor(x => x.AddressType).IsInEnum();
        address.RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        address.RuleFor(x => x.Line2).MaximumLength(200);
        address.RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        address.RuleFor(x => x.Region).MaximumLength(100);
        address.RuleFor(x => x.PostalCode).MaximumLength(20);
        address.RuleFor(x => x.CountryCode).NotEmpty().Matches(CountryPattern);
    }

    public static IReadOnlyList<SupplierContactInput> Contacts(IReadOnlyList<SupplierContactRequest>? contacts) =>
        (contacts ?? []).Select(c => new SupplierContactInput(c.Name, c.JobTitle, c.Phone, c.Email, c.IsPrimary)).ToList();

    public static IReadOnlyList<SupplierAddressInput> Addresses(IReadOnlyList<SupplierAddressRequest>? addresses) =>
        (addresses ?? []).Select(a => new SupplierAddressInput(
            a.AddressType, a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.CountryCode, a.IsPrimary)).ToList();
}
