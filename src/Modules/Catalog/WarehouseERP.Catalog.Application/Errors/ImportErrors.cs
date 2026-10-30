using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class ImportErrors
{
    public static readonly Error NotPermitted = Error.Forbidden(
        "Catalog.Import.NotPermitted", "Importing needs a permission to create or change catalog data.");

    public static readonly Error HasErrors = Error.BusinessRule(
        "Catalog.Import.HasErrors", "The file has errors, so nothing was imported. Preview it again to see them.");

    public static readonly Error NothingToImport = Error.BusinessRule(
        "Catalog.Import.NothingToImport", "No row would create or change anything.");

    public static readonly Error ChangedSincePreview = Error.Conflict(
        "Catalog.Import.ChangedSincePreview",
        "The file, the mode or the catalog changed since the preview, so nothing was imported. Preview it again and confirm.");
}
