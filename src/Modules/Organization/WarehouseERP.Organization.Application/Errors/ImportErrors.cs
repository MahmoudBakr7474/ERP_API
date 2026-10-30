using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

public static class ImportErrors
{
    public static readonly Error NotPermitted = Error.Forbidden(
        "Organization.Import.NotPermitted", "Importing locations needs the permission to add or rename locations.");

    public static readonly Error HasErrors = Error.BusinessRule(
        "Organization.Import.HasErrors", "The file has errors, so nothing was imported. Preview it again to see them.");

    public static readonly Error NothingToImport = Error.BusinessRule(
        "Organization.Import.NothingToImport", "No row would add or rename a location.");

    public static readonly Error ChangedSincePreview = Error.Conflict(
        "Organization.Import.ChangedSincePreview",
        "The file, the mode or the locations changed since the preview, so nothing was imported. Preview it again and confirm.");
}
