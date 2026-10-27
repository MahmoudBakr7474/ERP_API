using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("Identity.User.NotFound", "The user does not exist.");

    public static readonly Error UserNameTaken = Error.Conflict(
        "Identity.User.UserNameTaken", "Another user already has this user name.");

    public static readonly Error CannotDeactivateSelf = Error.BusinessRule(
        "Identity.User.CannotDeactivateSelf", "You can't deactivate your own account.");

    public static readonly Error CannotResetOwnPassword = Error.BusinessRule(
        "Identity.User.CannotResetOwnPassword", "Change your own password with your current password (POST /api/identity/me/password).");

    public static readonly Error CannotUnlockSelf = Error.BusinessRule(
        "Identity.User.CannotUnlockSelf", "Another administrator must unlock your account, or the lockout ends by itself.");

    public static readonly Error LastAdministrator = Error.BusinessRule(
        "Identity.User.LastAdministrator", "At least one active, company-wide user must keep the Administrator role.");
}
