namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>
/// Stable codes of row problems only an import has. Problems the single-record endpoints share keep those endpoints'
/// codes (Organization.Warehouse.NotFound, Organization.WarehouseLocation.CodeTaken...).
/// </summary>
public static class ImportIssueCodes
{
    public const string Required = "Organization.Import.Required";
    public const string TooLong = "Organization.Import.TooLong";
    public const string InvalidFormat = "Organization.Import.InvalidFormat";
    public const string DuplicateInFile = "Organization.Import.DuplicateInFile";
    public const string NotPermitted = "Organization.Import.NotPermitted";
    public const string MissingColumn = "Organization.Import.MissingColumn";
    public const string NoRows = "Organization.Import.NoRows";

    /// <summary>Transit warehouses' locations are opened and retired by stock transfers, never by hand.</summary>
    public const string TransitWarehouse = "Organization.Import.TransitWarehouse";

    /// <summary>The DEFAULT location is created with its warehouse and is never changed by an import.</summary>
    public const string DefaultLocationFixed = "Organization.Import.DefaultLocationFixed";
}
