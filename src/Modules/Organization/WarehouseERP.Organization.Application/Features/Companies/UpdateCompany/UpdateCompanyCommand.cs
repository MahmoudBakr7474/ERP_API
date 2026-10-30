namespace WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;

/// <summary>
/// The settings that can change; code and base currency are fixed at setup. The contact details are replaced as
/// sent: an omitted one is cleared.
/// </summary>
/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
/// <param name="CityId">A city of the address lookups. A new one must be active with its governorate and country; the
/// current one may stay even after it was retired.</param>
public sealed record UpdateCompanyCommand(
    string NameAr, string? NameEn, string TimeZoneId, string? TaxRegistrationNumber, string RowVersion,
    string? CommercialRegistrationNumber = null, string? CommercialRegistry = null, string? Phone = null, string? Email = null,
    string? AddressLine = null, Guid? CityId = null);
