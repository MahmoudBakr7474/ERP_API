using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Customers;

/// <summary>An address of the customer.</summary>
/// <param name="CityId">A city of Organization's lookups (its governorate and country follow); a new one must be active.</param>
/// <param name="IsPrimary">The main address; at most one.</param>
public sealed record CustomerAddressRequest(
    CustomerAddressType AddressType, string Line1, string? Line2, Guid CityId, string? PostalCode, bool IsPrimary = false);
