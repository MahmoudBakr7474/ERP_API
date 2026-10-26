using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Domain;

public static class IdentityErrors
{
    public static readonly Error SystemRoleLocked = Error.BusinessRule(
        "Identity.Role.SystemRoleLocked", "A system role can't be changed or deleted.");

    public static Error UnknownPermissions(IEnumerable<string> names) => Error.BusinessRule(
        "Identity.Role.UnknownPermission", "Unknown permissions: " + string.Join(", ", names) + ".");

    public static readonly Error SessionRevoked = Error.Unauthorized(
        "Identity.Session.Revoked", "The session has ended. Sign in again.");

    public static readonly Error RefreshTokenReused = Error.Unauthorized(
        "Identity.Session.RefreshTokenReused", "This refresh token was already used; the session has been ended.");
}
