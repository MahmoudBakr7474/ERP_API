using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <summary>An address of the supplier, as the client sends it.</summary>
/// <param name="CountryCode">ISO 3166 alpha-2.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record SupplierAddressRequest(
    SupplierAddressType AddressType, string Line1, string? Line2, string City, string? Region, string? PostalCode,
    string CountryCode, bool IsPrimary = false);
