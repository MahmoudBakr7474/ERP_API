namespace WarehouseERP.Organization.Application.Features.Geography.SetCountryActive;

public sealed record SetCountryActiveCommand(Guid CountryId, bool IsActive);
