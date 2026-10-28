using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>One of the supplier's addresses.</summary>
/// <param name="CityId">A city of Organization's lookups; its governorate and country follow from it.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record SupplierAddressInput(
    SupplierAddressType AddressType, string Line1, string? Line2, Guid CityId, string? PostalCode, bool IsPrimary);
