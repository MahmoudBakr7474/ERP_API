using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>Why an uploaded file can't be read at all (400, on the <c>file</c> field). Codes are stable.</summary>
public static class ImportFileErrors
{
    public static readonly Error Missing = Field("Import.File.Missing", "Choose a file to upload.");

    public static readonly Error UnsupportedType = Field(
        "Import.File.UnsupportedType", "Only Excel workbooks (.xlsx) and CSV files (.csv) can be imported.");

    public static readonly Error Unreadable = Field(
        "Import.File.Unreadable", "The file could not be read. Save it again as .xlsx (or UTF-8 .csv) and retry.");

    public static readonly Error Empty = Field("Import.File.Empty", "The file has no header row.");

    public static Error TooLarge(long maxBytes) => Field(
        "Import.File.TooLarge", $"The file is larger than {maxBytes / (1024 * 1024)} MB. Split it into smaller files.");

    public static Error TooManyRows(string sheet, int maxRows) => Field(
        "Import.File.TooManyRows", $"Sheet \"{sheet}\" has more than {maxRows} rows. Split it into smaller files.");

    public static Error TooManyColumns(string sheet, int maxColumns) => Field(
        "Import.File.TooManyColumns", $"Sheet \"{sheet}\" has more than {maxColumns} columns.");

    private static Error Field(string code, string message) => Error.Validation([new ValidationError("file", code, message)]);
}
