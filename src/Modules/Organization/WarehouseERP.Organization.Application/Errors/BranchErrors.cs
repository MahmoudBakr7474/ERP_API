using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

public static class BranchErrors
{
    // A command that names a branch that is missing or inactive breaks a rule of the request
    // (422); 404 is kept for the resource the URL addresses.
    public static readonly Error NotActive = Error.BusinessRule(
        "Organization.Branch.NotActive", "The branch does not exist or is inactive.");
}
