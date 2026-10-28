using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Suppliers;

/// <summary>An address with its city, governorate and country as Organization names them now.</summary>
public sealed record SupplierAddressResponse(
    Guid Id, SupplierAddressType AddressType, string Line1, string? Line2,
    string CountryCode, string CountryNameAr, string? CountryNameEn,
    Guid GovernorateId, string GovernorateNameAr, string? GovernorateNameEn,
    Guid CityId, string CityNameAr, string? CityNameEn,
    string? PostalCode, bool IsPrimary);
