using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Domain;

public static class SessionDomainErrors
{
    public static readonly Error Revoked = Error.Unauthorized(
        "Identity.Session.Revoked", "The session has ended. Sign in again.");

    public static readonly Error RefreshTokenReused = Error.Unauthorized(
        "Identity.Session.RefreshTokenReused", "This refresh token was already used; the session has been ended.");
}
