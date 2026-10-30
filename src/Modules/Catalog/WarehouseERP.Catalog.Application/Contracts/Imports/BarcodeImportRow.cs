namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>A row of the Barcodes sheet: the product's SKU, the unit the barcode is printed on, the value and its type.</summary>
public sealed record BarcodeImportRow(int Row, string? Sku, string? UnitCode, string? Barcode, string? BarcodeType);
