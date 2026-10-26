using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

public static class PermissionErrors
{
    public static readonly Error ExceedsOwnPermissions = Error.Forbidden(
        "Identity.Access.ExceedsOwnPermissions",
        "You can't grant permissions you don't hold, or manage a user who holds them.");
}
