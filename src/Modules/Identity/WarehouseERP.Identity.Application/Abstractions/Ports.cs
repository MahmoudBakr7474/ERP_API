using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions;

// Ports implemented in WarehouseERP.Identity: ASP.NET Core Identity and EF Core stay behind them. None of them
// saves: every use case ends with one IIdentityPersistence.SaveChangesAsync, one database transaction.

/// <summary>User accounts and credentials (ASP.NET Core Identity).</summary>
public interface IUserAccounts
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct);

    /// <summary>Checks credentials; a wrong password counts toward lockout (persisted at the save point).</summary>
    Task<LoginCheck> CheckPasswordAsync(string userName, string password, CancellationToken ct);

    /// <summary>Creates the account; password policy failures come back as validation errors.</summary>
    Task<Result<Guid>> CreateAsync(NewUserAccount account, CancellationToken ct);

    Task<Result> UpdateProfileAsync(Guid userId, string displayName, string? email, CancellationToken ct);

    Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct);

    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);

    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);

    Task<Result> UnlockAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken ct);

    /// <summary>Replaces the user's roles.</summary>
    Task<Result> SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct);

    Task<int> CountActiveUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct);

    Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken ct);
}

public sealed record UserAccount(Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut);

public sealed record NewUserAccount(string UserName, string DisplayName, string? Email, string Password);

public enum LoginOutcome
{
    Succeeded,
    UnknownUser,
    WrongPassword,
    LockedOut,
    Inactive,
}

public sealed record LoginCheck(LoginOutcome Outcome, Guid? UserId);

public interface IRoleRepository
{
    Task<Role?> GetAsync(Guid roleId, CancellationToken ct);

    Task<Guid?> FindAdministratorIdAsync(CancellationToken ct);

    Task<bool> NameExistsAsync(string name, Guid? excludingRoleId, CancellationToken ct);

    /// <summary>The roles among these ids that exist.</summary>
    Task<IReadOnlyList<Role>> GetManyAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct);

    /// <summary>Tracks a new role and returns its id.</summary>
    Task<Guid> AddAsync(Role role, CancellationToken ct);

    Task UpdateAsync(Role role, CancellationToken ct);

    Task DeleteAsync(Guid roleId, CancellationToken ct);

    Task<IReadOnlyList<RoleSummary>> ListAsync(CancellationToken ct);
}

public sealed record RoleSummary(Guid Id, string Name, string? Description, bool IsSystem, int PermissionCount);

public interface IUserAccessRepository
{
    Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct);

    void Add(UserAccess access);
}

public interface ISessionRepository
{
    void Add(UserSession session);

    Task<UserSession?> GetAsync(Guid sessionId, CancellationToken ct);

    /// <summary>The session whose current or previous refresh token has this hash.</summary>
    Task<UserSession?> FindByRefreshHashAsync(byte[] refreshHash, CancellationToken ct);

    /// <summary>Active sessions of a user, tracked, to end them.</summary>
    Task<IReadOnlyList<UserSession>> GetActiveAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<SessionItem>> ListAsync(Guid userId, CancellationToken ct);
}

public sealed record SessionItem(
    Guid Id, SessionClientType ClientType, string? DeviceId, DateTime CreatedAtUtc, DateTime LastSeenAtUtc, DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc, SessionRevokeReason? RevokedReason);

public interface IUserReadStore
{
    Task<UserDetails?> GetAsync(Guid userId, CancellationToken ct);

    Task<PagedResult<UserSummary>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
}

public sealed record NamedItem(Guid Id, string Name);

public sealed record UserAccessDetails(bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);

public sealed record UserDetails(
    Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut,
    IReadOnlyList<NamedItem> Roles, UserAccessDetails Access);

public sealed record UserSummary(Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut);

/// <summary>The module's save point: one SaveChanges for everything the use case changed.</summary>
public interface IIdentityPersistence
{
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
