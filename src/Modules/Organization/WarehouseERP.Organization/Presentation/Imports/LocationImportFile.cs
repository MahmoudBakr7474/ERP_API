using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Features.Imports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Presentation.Imports;

// Reads an uploaded workbook (its Locations sheet, found by name in either language; a workbook of one sheet is read
// whatever its name) or a CSV file into import rows. The template's instructions and lists are ignored. A Locations
// sheet without one of its required columns is reported and its rows are left out.
internal static class LocationImportFile
{
    public static readonly ImportLimits Limits = new();

    public static async Task<Result<LocationImportUpload>> ReadAsync(IFormFile? file, CancellationToken ct)
    {
        var read = await TabularFileReader.ReadAsync(file, Limits, ct);
        if (read.IsFailure)
            return read.Error;

        string fingerprint;
        await using (var content = file!.OpenReadStream())
            fingerprint = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, ct));

        var sheets = read.Value;
        var sheet = sheets.Count == 1 ? sheets[0] : sheets.FirstOrDefault(s => LocationImportLayout.IsLocationsSheet(s.Name));
        var rows = new List<LocationImportRow>();
        var issues = new List<LocationImportIssue>();
        if (sheet is null)
            return new LocationImportUpload(new LocationImportBatch(rows, issues), fingerprint);

        var (columns, missing) = ImportColumns.Map(LocationImportLayout.Columns, sheet.Headers);
        foreach (var column in missing)
            issues.Add(new LocationImportIssue(1, column.Key, ImportIssueCodes.MissingColumn,
                $"The sheet has no \"{column.HeaderEn}\" column. Use the template's header row.", null));
        if (missing.Count > 0)
            return new LocationImportUpload(new LocationImportBatch(rows, issues), fingerprint);

        foreach (var row in sheet.Rows)
        {
            string? V(string key) => columns.TryGetValue(key, out var at) ? row.Cells[at] : null;
            rows.Add(new LocationImportRow(row.Number, V("branchCode"), V("warehouseCode"), V("code"), V("nameAr"), V("nameEn")));
        }
        return new LocationImportUpload(new LocationImportBatch(rows, issues), fingerprint);
    }
}
