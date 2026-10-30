namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>
/// Everything an uploaded file holds, as text rows per sheet, plus what was wrong with the file's layout (a missing
/// column, no data). The file reader fills it; the import use cases parse and check every value.
/// </summary>
public sealed record CatalogImportBatch(
    IReadOnlyList<UnitImportRow> Units,
    IReadOnlyList<CategoryImportRow> Categories,
    IReadOnlyList<ProductImportRow> Products,
    IReadOnlyList<ProductUnitImportRow> ProductUnits,
    IReadOnlyList<BarcodeImportRow> Barcodes,
    IReadOnlyList<CatalogImportIssue> FileIssues)
{
    public int RowCount => Units.Count + Categories.Count + Products.Count + ProductUnits.Count + Barcodes.Count;
}
