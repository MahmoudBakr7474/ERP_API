using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>One of the customer's addresses.</summary>
/// <param name="CityId">A city of Organization's lookups; its governorate and country follow from it.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record CustomerAddressInput(
    CustomerAddressType AddressType, string Line1, string? Line2, Guid CityId, string? PostalCode, bool IsPrimary);
