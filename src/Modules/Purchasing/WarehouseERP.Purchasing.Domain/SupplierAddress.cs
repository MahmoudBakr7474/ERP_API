using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>An address of the supplier. Replaced with the supplier's other details; its history is a temporal table.</summary>
public sealed class SupplierAddress
{
    private SupplierAddress() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public SupplierAddressType AddressType { get; private set; }
    public string Line1 { get; private set; } = null!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }
    public string CountryCode { get; private set; } = null!;
    public bool IsPrimary { get; private set; }

    internal static Result<SupplierAddress> Create(SupplierAddressInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Line1);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.City);
        var country = input.CountryCode.Trim().ToUpperInvariant();
        if (country.Length != 2 || !country.All(char.IsAsciiLetterUpper))
            return SupplierDomainErrors.CountryInvalid;
        return new SupplierAddress
        {
            AddressType = input.AddressType, Line1 = input.Line1.Trim(), Line2 = Text.Optional(input.Line2), City = input.City.Trim(),
            Region = Text.Optional(input.Region), PostalCode = Text.Optional(input.PostalCode), CountryCode = country,
            IsPrimary = input.IsPrimary,
        };
    }
}
