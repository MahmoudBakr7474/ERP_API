namespace WarehouseERP.Identity.Domain;

/// <summary>What a <see cref="SecurityEvent"/> records. Stored as a number: values never change or get reused.</summary>
public enum SecurityEventType : byte
{
    SignedIn = 1,
    SignInFailed = 2,
    SignedOut = 3,
    ApiTokenIssued = 4,
    RefreshTokenReused = 5,
    PasswordChanged = 6,
    PasswordChangeRejected = 7,
    PasswordReset = 8,
    UserCreated = 9,
    UserUpdated = 10,
    UserActivated = 11,
    UserDeactivated = 12,
    UserUnlocked = 13,
    UserRolesChanged = 14,
    UserAccessChanged = 15,
    RoleCreated = 16,
    RoleUpdated = 17,
    RoleDeleted = 18,
    SessionRevoked = 19,
    UserSessionsRevoked = 20,
    PrivilegeEscalationBlocked = 21,
    AdministratorBootstrapped = 22,
    UserLockedOut = 23,
}
