using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Errors;

public static class UnitErrors
{
    // A command naming a missing or inactive unit breaks a rule of the request (422); 404 is kept for the
    // resource the URL addresses.
    public static readonly Error NotActive = Error.BusinessRule(
        "Catalog.Unit.NotActive", "The unit does not exist or is inactive.");
}
