using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

public static class BranchErrors
{
    // A command that names a branch that is missing or inactive breaks a rule of the request
    // (422); 404 is kept for the resource the URL addresses.
    public static readonly Error NotActive = Error.BusinessRule(
        "Organization.Branch.NotActive", "The branch does not exist or is inactive.");

    public static readonly Error CodeTaken = Error.Conflict(
        "Organization.Branch.CodeTaken", "Another branch already uses this code.");

    public static readonly Error NotFound = Error.NotFound(
        "Organization.Branch.NotFound", "The branch does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Organization.Branch.AccessDenied", "You are not allowed to access this branch.");

    // An inactive branch never holds an active warehouse.
    public static readonly Error HasActiveWarehouses = Error.BusinessRule(
        "Organization.Branch.HasActiveWarehouses", "The branch still has active warehouses; deactivate them first.");
}
