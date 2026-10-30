using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// Reads an uploaded .xlsx or .csv into plain text rows, within <see cref="ImportLimits"/>. It never evaluates
/// formulas (a formula cell gives its last saved value), never uses the uploaded name as a path, and checks an XLSX
/// archive's declared sizes before opening it. What the rows mean is for the importing module to decide.
/// </summary>
public static class TabularFileReader
{
    public static async Task<Result<IReadOnlyList<ImportSheet>>> ReadAsync(IFormFile? file, ImportLimits limits, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return ImportFileErrors.Missing;
        if (file.Length > limits.MaxFileBytes)
            return ImportFileErrors.TooLarge(limits.MaxFileBytes);
        // The extension only picks the parser; the content must still parse as that format.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".xlsx" or ".csv"))
            return ImportFileErrors.UnsupportedType;

        using var content = new MemoryStream((int)file.Length);
        await using (var upload = file.OpenReadStream())
            await upload.CopyToAsync(content, ct);
        if (content.Length > limits.MaxFileBytes)
            return ImportFileErrors.TooLarge(limits.MaxFileBytes);
        content.Position = 0;
        return extension == ".xlsx" ? ReadXlsx(content, limits) : ReadCsv(content, limits);
    }

    public static Result<IReadOnlyList<ImportSheet>> ReadXlsx(Stream content, ImportLimits limits)
    {
        try
        {
            if (!ArchiveIsSafe(content, limits))
                return ImportFileErrors.Unreadable;
            content.Position = 0;
            using var workbook = new XLWorkbook(content);
            var sheets = new List<ImportSheet>();
            foreach (var worksheet in workbook.Worksheets)
            {
                var used = worksheet.RangeUsed();
                if (used is null)
                    continue;
                var lastRow = used.LastRow().RowNumber();
                var lastColumn = used.LastColumn().ColumnNumber();
                if (lastColumn > limits.MaxColumns)
                    return ImportFileErrors.TooManyColumns(worksheet.Name, limits.MaxColumns);
                if (lastRow - 1 > limits.MaxRowsPerSheet)
                    return ImportFileErrors.TooManyRows(worksheet.Name, limits.MaxRowsPerSheet);

                var headers = Enumerable.Range(1, lastColumn)
                    .Select(c => Header(CellText(worksheet.Cell(1, c), limits.MaxCellLength))).ToList();
                var rows = new List<ImportRow>();
                for (var r = 2; r <= lastRow; r++)
                {
                    var cells = Enumerable.Range(1, lastColumn)
                        .Select(c => CellText(worksheet.Cell(r, c), limits.MaxCellLength)).ToList();
                    if (cells.Any(c => c is not null))
                        rows.Add(new ImportRow(r, cells));
                }
                sheets.Add(new ImportSheet(worksheet.Name, headers, rows));
            }
            return sheets.Count == 0 ? ImportFileErrors.Empty : sheets;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A corrupt or hostile workbook is the user's input, not a server fault.
            return ImportFileErrors.Unreadable;
        }
    }

    public static Result<IReadOnlyList<ImportSheet>> ReadCsv(Stream content, ImportLimits limits)
    {
        try
        {
            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true, // comma, or the semicolon Excel writes in many locales
                HasHeaderRecord = false,
                MissingFieldFound = null,
                BadDataFound = null,
            };
            using var csv = new CsvReader(reader, configuration);
            List<string>? headers = null;
            var rows = new List<ImportRow>();
            var number = 0;
            while (csv.Read())
            {
                number++;
                var record = csv.Parser.Record ?? [];
                if (headers is null)
                {
                    if (record.Length > limits.MaxColumns)
                        return ImportFileErrors.TooManyColumns("CSV", limits.MaxColumns);
                    headers = record.Select(h => Header(Clean(h, limits.MaxCellLength))).ToList();
                    continue;
                }
                if (rows.Count >= limits.MaxRowsPerSheet)
                    return ImportFileErrors.TooManyRows("CSV", limits.MaxRowsPerSheet);
                var cells = Enumerable.Range(0, headers.Count)
                    .Select(i => i < record.Length ? Clean(record[i], limits.MaxCellLength) : null).ToList();
                if (cells.Any(c => c is not null))
                    rows.Add(new ImportRow(number, cells));
            }
            return headers is null ? ImportFileErrors.Empty : new[] { new ImportSheet("CSV", headers, rows) };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ImportFileErrors.Unreadable;
        }
    }

    // Declared sizes are enforced by .NET while it inflates, so checking them up front bounds the work.
    private static bool ArchiveIsSafe(Stream content, ImportLimits limits)
    {
        if (content.Length < 4 || content.ReadByte() != 'P' || content.ReadByte() != 'K')
            return false;
        content.Position = 0;
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > limits.MaxZipEntries)
            return false;
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            total += entry.Length;
            if (total > limits.MaxUncompressedBytes)
                return false;
            if (entry.Length > 1024 * 1024 && entry.Length > (long)limits.MaxCompressionRatio * Math.Max(1, entry.CompressedLength))
                return false;
        }
        return true;
    }

    private static string? CellText(IXLCell cell, int maxLength)
    {
        var value = cell.HasFormula ? cell.CachedValue : cell.Value;
        var text = value.Type switch
        {
            XLDataType.Text => value.GetText(),
            XLDataType.Number => NumberText(value.GetNumber()),
            XLDataType.Boolean => value.GetBoolean() ? "TRUE" : "FALSE",
            XLDataType.DateTime => value.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            XLDataType.TimeSpan => value.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture),
            XLDataType.Error => "#ERROR",
            _ => null,
        };
        return Clean(text, maxLength);
    }

    // Excel stores numbers as doubles: whole numbers (codes typed without the text format) come back without a
    // decimal point or exponent; others with the 15 significant digits Excel itself shows.
    private static string NumberText(double number)
    {
        if (Math.Abs(number) < 1e15 && number == Math.Floor(number))
            return ((long)number).ToString(CultureInfo.InvariantCulture);
        try
        {
            return Convert.ToDecimal(number).ToString(CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return number.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    // Over-long cells are cut just past the limit: the module's length rule still reports them.
    private static string? Clean(string? text, int maxLength)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length > maxLength ? trimmed[..(maxLength + 1)] : trimmed;
    }

    // Template headers mark required columns with "*".
    private static string Header(string? text) => (text ?? "").TrimEnd('*', ' ').Trim();
}
