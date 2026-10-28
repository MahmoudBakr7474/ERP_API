namespace WarehouseERP.Organization.Application.Features.Geography.UpdateCity;

/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateCityCommand(Guid CityId, string NameAr, string? NameEn, string RowVersion);
