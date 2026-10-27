namespace WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;

/// <summary>The settings that can change; code and base currency are fixed at setup.</summary>
/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateCompanyCommand(
    string NameAr, string? NameEn, string TimeZoneId, string? TaxRegistrationNumber, string RowVersion);
