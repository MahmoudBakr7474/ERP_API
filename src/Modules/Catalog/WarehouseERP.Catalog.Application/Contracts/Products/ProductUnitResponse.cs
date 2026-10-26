namespace WarehouseERP.Catalog.Application.Contracts.Products;

public sealed record ProductUnitResponse(Guid Id, Guid UnitId, decimal ConversionFactor, bool IsActive);
