using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

// Where a user may act: company-wide, branches, warehouses.
public static class UserAccessErrors
{
    public static readonly Error UnknownBranches = Error.BusinessRule(
        "Identity.Access.UnknownBranch", "One or more branches do not exist.");

    public static readonly Error UnknownWarehouses = Error.BusinessRule(
        "Identity.Access.UnknownWarehouse", "One or more warehouses do not exist.");

    public static readonly Error ManageRequired = Error.Forbidden(
        "Identity.Access.ManageRequired", "Giving a new user branches, warehouses or company-wide access needs identity.access.manage.");

    public static readonly Error ExceedsOwnScope = Error.Forbidden(
        "Identity.Access.ExceedsOwnScope",
        "You can only grant company-wide access, branches and warehouses that are within your own access.");
}
