namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.SetWarehouseLocationActive;

public sealed record SetWarehouseLocationActiveCommand(Guid WarehouseId, Guid LocationId, bool IsActive);
