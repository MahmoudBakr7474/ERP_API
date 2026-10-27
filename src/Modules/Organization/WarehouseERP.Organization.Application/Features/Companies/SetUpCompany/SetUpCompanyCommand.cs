namespace WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;

/// <param name="BaseCurrencyCode">ISO 4217 code, e.g. EGP. Fixed once the company is set up.</param>
/// <param name="TimeZoneId">IANA time zone, e.g. Africa/Cairo.</param>
public sealed record SetUpCompanyCommand(
    string Code, string NameAr, string? NameEn, string BaseCurrencyCode, string TimeZoneId, string? TaxRegistrationNumber);
