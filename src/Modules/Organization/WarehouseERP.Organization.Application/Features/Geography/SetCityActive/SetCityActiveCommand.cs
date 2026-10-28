namespace WarehouseERP.Organization.Application.Features.Geography.SetCityActive;

public sealed record SetCityActiveCommand(Guid CityId, bool IsActive);
