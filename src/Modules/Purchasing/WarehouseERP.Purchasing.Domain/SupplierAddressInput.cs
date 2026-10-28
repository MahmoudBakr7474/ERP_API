using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One of the supplier's addresses.</summary>
/// <param name="CountryCode">ISO 3166 alpha-2 country of the address.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record SupplierAddressInput(
    SupplierAddressType AddressType, string Line1, string? Line2, string City, string? Region, string? PostalCode,
    string CountryCode, bool IsPrimary);
