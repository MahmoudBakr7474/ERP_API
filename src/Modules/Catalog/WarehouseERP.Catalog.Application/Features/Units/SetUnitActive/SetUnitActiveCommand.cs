namespace WarehouseERP.Catalog.Application.Features.Units.SetUnitActive;

public sealed record SetUnitActiveCommand(Guid UnitId, bool IsActive);
