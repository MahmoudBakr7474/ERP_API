using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Domain;

public static class RoleDomainErrors
{
    public static readonly Error SystemRoleLocked = Error.BusinessRule(
        "Identity.Role.SystemRoleLocked", "A system role can't be changed or deleted.");

    public static Error UnknownPermissions(IEnumerable<string> names) => Error.BusinessRule(
        "Identity.Role.UnknownPermission", "Unknown permissions: " + string.Join(", ", names) + ".");
}
