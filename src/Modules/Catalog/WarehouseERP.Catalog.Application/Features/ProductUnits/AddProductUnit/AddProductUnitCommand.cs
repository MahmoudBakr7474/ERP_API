namespace WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;

/// <param name="ConversionFactor">How many base units one of this unit holds (a carton of 12 pieces: 12).</param>
public sealed record AddProductUnitCommand(Guid ProductId, Guid UnitId, decimal ConversionFactor);
