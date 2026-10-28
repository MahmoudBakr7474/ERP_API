using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <summary>An address of the supplier, as the client sends it.</summary>
/// <param name="CityId">An active city (of an active governorate and country) from /api/organization/cities.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record SupplierAddressRequest(
    SupplierAddressType AddressType, string Line1, string? Line2, Guid CityId, string? PostalCode, bool IsPrimary = false);
