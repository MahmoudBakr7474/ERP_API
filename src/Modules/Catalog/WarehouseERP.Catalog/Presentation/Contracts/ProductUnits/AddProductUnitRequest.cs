namespace WarehouseERP.Catalog.Presentation.Contracts.ProductUnits;

public sealed record AddProductUnitRequest(Guid UnitId, decimal ConversionFactor);
