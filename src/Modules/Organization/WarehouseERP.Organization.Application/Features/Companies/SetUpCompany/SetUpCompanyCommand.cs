namespace WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;

/// <param name="BaseCurrencyCode">ISO 4217 code, e.g. EGP. Fixed once the company is set up.</param>
/// <param name="TimeZoneId">IANA time zone, e.g. Africa/Cairo.</param>
/// <param name="CommercialRegistrationNumber">Optional, like every contact detail below; empty ones are never printed.</param>
/// <param name="CityId">A city of the address lookups; it, its governorate and its country must be active.</param>
public sealed record SetUpCompanyCommand(
    string Code, string NameAr, string? NameEn, string BaseCurrencyCode, string TimeZoneId, string? TaxRegistrationNumber,
    string? CommercialRegistrationNumber = null, string? CommercialRegistry = null, string? Phone = null, string? Email = null,
    string? AddressLine = null, Guid? CityId = null);
