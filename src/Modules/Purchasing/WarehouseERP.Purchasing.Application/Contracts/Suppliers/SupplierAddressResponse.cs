using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

public sealed record SupplierAddressResponse(
    Guid Id, SupplierAddressType AddressType, string Line1, string? Line2, string City, string? Region, string? PostalCode,
    string CountryCode, bool IsPrimary);
