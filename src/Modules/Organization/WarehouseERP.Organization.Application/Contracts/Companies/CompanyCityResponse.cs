namespace WarehouseERP.Organization.Application.Contracts.Companies;

/// <summary>The city of the company's address with its governorate and country, for display and the address pickers.</summary>
public sealed record CompanyCityResponse(
    Guid Id, string NameAr, string? NameEn, Guid GovernorateId, string GovernorateNameAr, string? GovernorateNameEn,
    string CountryCode, string CountryNameAr, string? CountryNameEn);
