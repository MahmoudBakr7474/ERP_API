using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>What confirming the import does with one row; <paramref name="Key"/> is the row's code (SKU, SKU/unit...).</summary>
public sealed record CatalogImportRowResult(CatalogImportSheet Sheet, int Row, string? Key, CatalogImportAction Action);
