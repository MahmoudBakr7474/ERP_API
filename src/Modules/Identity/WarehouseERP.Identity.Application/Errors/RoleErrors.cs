using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

// Application rules about roles; the role invariants themselves are RoleDomainErrors.
public static class RoleErrors
{
    public static readonly Error NotFound = Error.NotFound("Identity.Role.NotFound", "The role does not exist.");

    public static readonly Error NameTaken = Error.Conflict("Identity.Role.NameTaken", "Another role already has this name.");

    public static readonly Error InUse = Error.Conflict(
        "Identity.Role.InUse", "The role is assigned to users; remove it from them first.");

    public static readonly Error UnknownRoles = Error.BusinessRule(
        "Identity.Role.UnknownRole", "One or more roles do not exist.");
}
