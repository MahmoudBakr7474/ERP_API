namespace WarehouseERP.Organization.Domain;

/// <param name="CommercialRegistrationNumber">The commercial register number, as registered.</param>
/// <param name="CommercialRegistry">The office that keeps the register entry, e.g. سجل تجاري القاهرة.</param>
/// <param name="AddressLine">Street, building and district; the city names the governorate and country.</param>
/// <param name="CityId">A city of the address lookups.</param>
public sealed record CompanyContactDetails(
    string? CommercialRegistrationNumber, string? CommercialRegistry, string? Phone, string? Email, string? AddressLine, Guid? CityId);
