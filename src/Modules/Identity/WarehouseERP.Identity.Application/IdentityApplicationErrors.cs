using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application;

public static class IdentityApplicationErrors
{
    // One answer for an unknown user, a wrong password, a locked-out or an inactive account, so user names
    // can't be probed; the real reason is only logged.
    public static readonly Error LoginFailed = Error.Unauthorized(
        "Identity.Login.Failed", "Wrong user name or password, or the account is temporarily locked.");

    public static readonly Error UserNotFound = Error.NotFound("Identity.User.NotFound", "The user does not exist.");

    public static readonly Error UserNameTaken = Error.Conflict(
        "Identity.User.UserNameTaken", "Another user already has this user name.");

    public static readonly Error CannotDeactivateSelf = Error.BusinessRule(
        "Identity.User.CannotDeactivateSelf", "You can't deactivate your own account.");

    public static readonly Error LastAdministrator = Error.BusinessRule(
        "Identity.User.LastAdministrator", "At least one active user must keep the Administrator role.");

    public static readonly Error PasswordIncorrect = Error.BusinessRule(
        "Identity.Password.Incorrect", "The current password is not correct.");

    public static readonly Error RoleNotFound = Error.NotFound("Identity.Role.NotFound", "The role does not exist.");

    public static readonly Error RoleNameTaken = Error.Conflict("Identity.Role.NameTaken", "Another role already has this name.");

    public static readonly Error RoleInUse = Error.Conflict(
        "Identity.Role.InUse", "The role is assigned to users; remove it from them first.");

    public static readonly Error UnknownRoles = Error.BusinessRule(
        "Identity.Role.UnknownRole", "One or more roles do not exist.");

    public static readonly Error UnknownBranches = Error.BusinessRule(
        "Identity.Access.UnknownBranch", "One or more branches do not exist.");

    public static readonly Error UnknownWarehouses = Error.BusinessRule(
        "Identity.Access.UnknownWarehouse", "One or more warehouses do not exist.");

    public static readonly Error AccessManageRequired = Error.Forbidden(
        "Identity.Access.ManageRequired", "Giving a new user branches, warehouses or company-wide access needs identity.access.manage.");

    public static readonly Error ExceedsOwnPermissions = Error.Forbidden(
        "Identity.Access.ExceedsOwnPermissions",
        "You can't grant permissions you don't hold, or manage a user who holds them.");

    public static readonly Error ExceedsOwnScope = Error.Forbidden(
        "Identity.Access.ExceedsOwnScope",
        "You can only grant company-wide access, branches and warehouses that are within your own access.");

    public static readonly Error SessionNotFound = Error.NotFound("Identity.Session.NotFound", "The session does not exist.");

    public static readonly Error SessionAccessDenied = Error.Forbidden(
        "Identity.Session.AccessDenied", "You can only see and end your own sessions.");
}
