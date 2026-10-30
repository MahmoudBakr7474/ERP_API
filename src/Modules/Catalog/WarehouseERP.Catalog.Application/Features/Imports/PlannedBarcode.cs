using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>A Barcodes row after checking.</summary>
public sealed record PlannedBarcode(int Row, string? Sku, string? UnitCode, string? Barcode, BarcodeType Type, CatalogImportAction Action);
