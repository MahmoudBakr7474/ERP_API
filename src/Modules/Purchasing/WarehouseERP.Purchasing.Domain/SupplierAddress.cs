using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// An address of the supplier. Replaced with the supplier's other details; its history is a temporal table. It names a
/// city of Organization's lookups only: the governorate and the country are the city's, so they can't disagree with it.
/// </summary>
public sealed class SupplierAddress
{
    private SupplierAddress() { } // EF Core

    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public SupplierAddressType AddressType { get; private set; }
    public string Line1 { get; private set; } = null!;
    public string? Line2 { get; private set; }
    public Guid CityId { get; private set; }
    public string? PostalCode { get; private set; }
    public bool IsPrimary { get; private set; }

    internal static SupplierAddress Create(SupplierAddressInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.Line1);
        if (input.CityId == Guid.Empty)
            throw new ArgumentException("An address names its city.", nameof(input));
        return new SupplierAddress
        {
            AddressType = input.AddressType, Line1 = input.Line1.Trim(), Line2 = Text.Optional(input.Line2), CityId = input.CityId,
            PostalCode = Text.Optional(input.PostalCode), IsPrimary = input.IsPrimary,
        };
    }
}
