namespace WarehouseERP.Catalog.Application.Features.ProductUnits.SetProductUnitActive;

public sealed record SetProductUnitActiveCommand(Guid ProductId, Guid ProductUnitId, bool IsActive);
