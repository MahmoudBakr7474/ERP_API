using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>A ProductUnits row after checking.</summary>
public sealed record PlannedProductUnit(int Row, string? Sku, string? UnitCode, decimal Factor, CatalogImportAction Action);
