namespace WarehouseERP.Organization.Application.Features.Warehouses.SetWarehouseActive;

public sealed record SetWarehouseActiveCommand(Guid WarehouseId, bool IsActive);
