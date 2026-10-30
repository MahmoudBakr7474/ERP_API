namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>A row of the ProductUnits sheet: the product's SKU, a unit code and how many base units one of it holds.</summary>
public sealed record ProductUnitImportRow(int Row, string? Sku, string? UnitCode, string? ConversionFactor);
