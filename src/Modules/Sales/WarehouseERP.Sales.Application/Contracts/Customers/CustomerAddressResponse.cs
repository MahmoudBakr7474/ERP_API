using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Customers;

/// <summary>An address with its city, governorate and country names as the lookups have them now.</summary>
public sealed record CustomerAddressResponse(
    Guid Id, CustomerAddressType AddressType, string Line1, string? Line2, string CountryCode, string CountryNameAr,
    string? CountryNameEn, Guid GovernorateId, string GovernorateNameAr, string? GovernorateNameEn, Guid CityId, string CityNameAr,
    string? CityNameEn, string? PostalCode, bool IsPrimary);
