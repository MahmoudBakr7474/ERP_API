using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application;

public static class OrganizationErrors
{
    // A command that names a branch that is missing or inactive breaks a rule of the request
    // (422); 404 is kept for the resource the URL addresses.
    public static readonly Error BranchNotActive = Error.BusinessRule(
        "Organization.Branch.NotActive", "The branch does not exist or is inactive.");

    public static readonly Error WarehouseCodeTaken = Error.Conflict(
        "Organization.Warehouse.CodeTaken", "Another warehouse in this branch already uses this code.");

    public static readonly Error WarehouseNotFound = Error.NotFound(
        "Organization.Warehouse.NotFound", "The warehouse does not exist.");

    public static readonly Error WarehouseAccessDenied = Error.Forbidden(
        "Organization.Warehouse.AccessDenied", "You are not allowed to access this warehouse.");
}
