namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCountry;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateCountryCommand(Guid CountryId, string NameAr, string? NameEn, string RowVersion);
