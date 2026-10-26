namespace WarehouseERP.Identity.Domain;

public enum SessionRevokeReason : byte
{
    Logout = 1,
    RevokedByUser = 2,
    RevokedByAdministrator = 3,
    PasswordChanged = 4,
    PasswordReset = 5,
    UserDeactivated = 6,
    RefreshTokenReused = 7,
}
