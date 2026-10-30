namespace WarehouseERP.Organization.Application.Contracts.Companies;

/// <param name="RowVersion">Base64 row version; send it back with the next update.</param>
/// <param name="City">The address's city, governorate and country; null when no city is set.</param>
public sealed record CompanyResponse(
    Guid Id, string Code, string NameAr, string? NameEn, string BaseCurrencyCode, string TimeZoneId,
    string? TaxRegistrationNumber, string RowVersion, string? CommercialRegistrationNumber, string? CommercialRegistry,
    string? Phone, string? Email, string? AddressLine, CompanyCityResponse? City);
