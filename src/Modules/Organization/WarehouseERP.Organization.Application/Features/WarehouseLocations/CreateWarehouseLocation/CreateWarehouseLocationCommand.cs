namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;

public sealed record CreateWarehouseLocationCommand(Guid WarehouseId, string Code, string NameAr, string? NameEn);
