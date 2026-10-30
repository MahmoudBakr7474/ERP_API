using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Imports;
using WarehouseERP.Catalog.Application.Features.Imports;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation.Imports;

// Reads an uploaded workbook (the template's sheets, found by name) or a CSV file (one kind of rows, named by the
// request) into import rows. Sheets it doesn't know, such as the template's instructions and lists, are ignored; a
// known sheet without one of its required columns is reported and its rows are left out.
internal static class CatalogImportFile
{
    public static readonly ImportLimits Limits = new();

    public static async Task<Result<CatalogImportUpload>> ReadAsync(IFormFile? file, CatalogImportSheet? csvSheet, CancellationToken ct)
    {
        var read = await TabularFileReader.ReadAsync(file, Limits, ct);
        if (read.IsFailure)
            return read.Error;
        var isCsv = string.Equals(Path.GetExtension(file!.FileName), ".csv", StringComparison.OrdinalIgnoreCase);
        if (isCsv && csvSheet is not { } kind || csvSheet is { } k && !Enum.IsDefined(k))
            return Error.Validation([new ValidationError("sheet", "Catalog.Import.SheetRequired",
                "Choose which rows the CSV file holds: Units, Categories, Products, ProductUnits or Barcodes.")]);

        string fingerprint;
        await using (var content = file.OpenReadStream())
            fingerprint = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, ct)) + (isCsv ? $"|{csvSheet}" : "");

        var issues = new List<CatalogImportIssue>();
        var units = new List<UnitImportRow>();
        var categories = new List<CategoryImportRow>();
        var products = new List<ProductImportRow>();
        var productUnits = new List<ProductUnitImportRow>();
        var barcodes = new List<BarcodeImportRow>();
        var seen = new HashSet<CatalogImportSheet>();

        foreach (var sheet in read.Value)
        {
            var layout = isCsv ? CatalogImportLayout.For(csvSheet!.Value) : CatalogImportLayout.Find(sheet.Name);
            if (layout is null)
                continue;
            if (!seen.Add(layout.Sheet))
            {
                issues.Add(new CatalogImportIssue(layout.Sheet, 1, null, ImportIssueCodes.DuplicateSheet,
                    $"The workbook has the {layout.NameEn} sheet twice; keep one.", sheet.Name));
                continue;
            }
            if (Columns(layout, sheet, issues) is not { } columns)
                continue;

            foreach (var row in sheet.Rows)
            {
                string? V(string key) =>
                    columns.TryGetValue(key, out var at) ? CatalogImportLayout.Value(layout.Columns.First(c => c.Key == key), row.Cells[at]) : null;
                switch (layout.Sheet)
                {
                    case CatalogImportSheet.Units:
                        units.Add(new UnitImportRow(row.Number, V("code"), V("nameAr"), V("nameEn"), V("allowsFraction")));
                        break;
                    case CatalogImportSheet.Categories:
                        categories.Add(new CategoryImportRow(row.Number, V("code"), V("nameAr"), V("nameEn"), V("parentCode")));
                        break;
                    case CatalogImportSheet.Products:
                        products.Add(new ProductImportRow(row.Number, V("sku"), V("nameAr"), V("nameEn"), V("baseUnitCode"),
                            V("categoryCode"), V("type"), V("trackingMode")));
                        break;
                    case CatalogImportSheet.ProductUnits:
                        productUnits.Add(new ProductUnitImportRow(row.Number, V("sku"), V("unitCode"), V("conversionFactor")));
                        break;
                    default:
                        barcodes.Add(new BarcodeImportRow(row.Number, V("sku"), V("unitCode"), V("barcode"), V("barcodeType")));
                        break;
                }
            }
        }

        return new CatalogImportUpload(new CatalogImportBatch(units, categories, products, productUnits, barcodes, issues), fingerprint);
    }

    // Header text -> cell index, or null (with an issue per missing column) when a required column is absent.
    private static Dictionary<string, int>? Columns(ImportSheetLayout layout, ImportSheet sheet, List<CatalogImportIssue> issues)
    {
        var (columns, missing) = ImportColumns.Map(layout.Columns, sheet.Headers);
        foreach (var column in missing)
            issues.Add(new CatalogImportIssue(layout.Sheet, 1, column.Key, ImportIssueCodes.MissingColumn,
                $"The {layout.NameEn} sheet has no \"{column.HeaderEn}\" column. Use the template's header row.", null));
        return missing.Count == 0 ? columns : null;
    }
}
