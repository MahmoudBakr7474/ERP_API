namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>
/// Stable codes of row problems only an import has (shape, duplicates, references between sheets). Problems the
/// single-record endpoints share keep those endpoints' codes (Catalog.Product.SkuTaken, Catalog.Unit.NotActive...).
/// </summary>
public static class ImportIssueCodes
{
    public const string Required = "Catalog.Import.Required";
    public const string TooLong = "Catalog.Import.TooLong";
    public const string InvalidFormat = "Catalog.Import.InvalidFormat";
    public const string InvalidValue = "Catalog.Import.InvalidValue";
    public const string InvalidNumber = "Catalog.Import.InvalidNumber";
    public const string DuplicateInFile = "Catalog.Import.DuplicateInFile";
    public const string NotPermitted = "Catalog.Import.NotPermitted";
    public const string FixedField = "Catalog.Import.FixedField";
    public const string ReferenceInvalid = "Catalog.Import.ReferenceInvalid";
    public const string BaseUnitFactor = "Catalog.Import.BaseUnitFactor";
    public const string BarcodeInvalid = "Catalog.Import.BarcodeInvalid";
    public const string MissingColumn = "Catalog.Import.MissingColumn";
    public const string NoRows = "Catalog.Import.NoRows";
    public const string DuplicateSheet = "Catalog.Import.DuplicateSheet";
}
