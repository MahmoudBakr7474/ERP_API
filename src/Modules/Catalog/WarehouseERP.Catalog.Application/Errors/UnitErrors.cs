using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class UnitErrors
{
    // A command naming a missing or inactive unit breaks a rule of the request (422); 404 is kept for the
    // resource the URL addresses.
    public static readonly Error NotActive = Error.BusinessRule(
        "Catalog.Unit.NotActive", "The unit does not exist or is inactive.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Catalog.Unit.CodeTaken", "Another unit already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Catalog.Unit.NotFound", "The unit does not exist.");

    public static readonly Error InUse = Error.BusinessRule(
        "Catalog.Unit.InUse", "Active products still use this unit; deactivate those products or their unit first.");
}
