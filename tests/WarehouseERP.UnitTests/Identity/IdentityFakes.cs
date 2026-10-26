using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.Roles;
using WarehouseERP.Identity.Application.Contracts.Sessions;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.Identity;

// In-memory ports for Identity's handlers: enough state to drive each rule, plus what the handler did with it.

/// <param name="permissions">Null: holds every permission (the Administrator).</param>
internal sealed class FakeCaller(IEnumerable<string>? permissions = null, bool allWarehouses = true, IEnumerable<Guid>? warehouses = null)
    : ICurrentUser
{
    private readonly HashSet<string>? _permissions = permissions?.ToHashSet();

    public Guid? UserId { get; } = Guid.NewGuid();
    public bool IsAuthenticated => true;
    public bool HasPermission(string permission) => _permissions is null || _permissions.Contains(permission);
    public bool HasAllWarehouses { get; } = allWarehouses;
    public IReadOnlySet<Guid> WarehouseIds { get; } = (warehouses ?? []).ToHashSet();
}

// Accounts, credentials and role assignments in one fake: the handlers see three narrow ports over one state.
internal sealed class FakeAccounts : IUserAccounts, IUserCredentials, IUserRoleAssignments
{
    public Dictionary<Guid, UserAccount> Users { get; } = [];
    public Dictionary<Guid, List<Guid>> UserRoles { get; } = [];
    public LoginCheck NextLogin { get; set; } = new(LoginOutcome.Succeeded, Guid.NewGuid());
    public Result NextPasswordChange { get; set; } = Result.Success();
    public List<string> Changes { get; } = [];

    public Guid Add(params Guid[] roleIds)
    {
        var id = Guid.NewGuid();
        Users[id] = new UserAccount(id, "user-" + id.ToString("N")[..6], "User", null, IsActive: true, IsLockedOut: false);
        UserRoles[id] = [.. roleIds];
        return id;
    }

    public Task<UserAccount?> FindAsync(Guid userId, CancellationToken ct) => Task.FromResult(Users.GetValueOrDefault(userId));

    public Task<LoginCheck> CheckPasswordAsync(string userName, string password, CancellationToken ct) => Task.FromResult(NextLogin);

    public Task<Result<Guid>> CreateAsync(NewUserAccount account, CancellationToken ct)
    {
        var id = Add();
        Changes.Add("create " + account.UserName);
        return Task.FromResult(Result.Success(id));
    }

    public Task<Result> UpdateProfileAsync(Guid userId, string displayName, string? email, CancellationToken ct) => Change(userId, "profile");

    public Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct) => Change(userId, isActive ? "activate" : "deactivate");

    public Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct) => Change(userId, "reset");

    public Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct) =>
        Task.FromResult(NextPasswordChange);

    public Task<Result> UnlockAsync(Guid userId, CancellationToken ct) => Change(userId, "unlock");

    public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(UserRoles.GetValueOrDefault(userId) ?? []);

    public Task<Result> SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        if (!Users.ContainsKey(userId))
            return Task.FromResult<Result>(UserErrors.NotFound);
        UserRoles[userId] = [.. roleIds];
        Changes.Add("roles");
        return Task.FromResult(Result.Success());
    }

    public Task<int> CountActiveUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct) =>
        Task.FromResult(UserRoles.Count(u => u.Key != excludingUserId && Users[u.Key].IsActive && u.Value.Contains(roleId)));

    public Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken ct) => Task.FromResult(UserRoles.Values.Any(r => r.Contains(roleId)));

    private Task<Result> Change(Guid userId, string what)
    {
        if (!Users.ContainsKey(userId))
            return Task.FromResult<Result>(UserErrors.NotFound);
        Changes.Add(what);
        return Task.FromResult(Result.Success());
    }
}

internal sealed class FakeRoles : IRoleRepository
{
    public Dictionary<Guid, Role> Roles { get; } = [];
    public List<Role> Added { get; } = [];

    public Guid AdministratorId { get; }

    public FakeRoles() => AdministratorId = Add(Role.AdministratorName, isSystem: true);

    public Guid Add(string name, bool isSystem = false, params string[] permissions)
    {
        var id = Guid.NewGuid();
        Roles[id] = Role.Restore(id, name, null, isSystem, permissions);
        return id;
    }

    public Task<Role?> GetAsync(Guid roleId, CancellationToken ct) => Task.FromResult(Roles.GetValueOrDefault(roleId));

    public Task<Guid?> FindAdministratorIdAsync(CancellationToken ct) => Task.FromResult<Guid?>(AdministratorId);

    public Task<bool> NameExistsAsync(string name, Guid? excludingRoleId, CancellationToken ct) =>
        Task.FromResult(Roles.Values.Any(r => r.Id != excludingRoleId && string.Equals(r.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Role>> GetManyAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Role>>(roleIds.Distinct().Where(Roles.ContainsKey).Select(id => Roles[id]).ToList());

    public Task<Guid> AddAsync(Role role, CancellationToken ct)
    {
        Added.Add(role);
        return Task.FromResult(Guid.NewGuid());
    }

    public Task UpdateAsync(Role role, CancellationToken ct) => Task.CompletedTask;

    public Task DeleteAsync(Guid roleId, CancellationToken ct) => Task.FromResult(Roles.Remove(roleId));

    public Task<IReadOnlyList<RoleSummaryResponse>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
}

internal sealed class FakeAccess : IUserAccessRepository
{
    public Dictionary<Guid, UserAccess> ByUser { get; } = [];

    public Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct) => Task.FromResult(ByUser.GetValueOrDefault(userId));

    public void Add(UserAccess access) => ByUser[access.UserId] = access;
}

internal sealed class FakeSessions : ISessionRepository
{
    public List<UserSession> All { get; } = [];

    public UserSession StartWeb(Guid userId, DateTime now)
    {
        var session = UserSession.StartWeb(userId, SessionTokens.Create().Hash, now, TimeSpan.FromHours(12));
        Add(session);
        return session;
    }

    // EF Core generates the key when a session is added; the fake does the same.
    public void Add(UserSession session)
    {
        typeof(UserSession).GetProperty(nameof(UserSession.Id))!.SetValue(session, Guid.NewGuid());
        All.Add(session);
    }

    public Task<UserSession?> GetAsync(Guid sessionId, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(s => s.Id == sessionId));

    public Task<UserSession?> FindByRefreshHashAsync(byte[] refreshHash, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(s =>
            (s.RefreshTokenHash is { } current && SessionTokens.HashesEqual(current, refreshHash))
            || (s.PreviousRefreshTokenHash is { } previous && SessionTokens.HashesEqual(previous, refreshHash))));

    public Task<IReadOnlyList<UserSession>> GetActiveAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserSession>>(All.Where(s => s.UserId == userId && !s.IsRevoked).ToList());

    public Task<IReadOnlyList<SessionResponse>> ListAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SessionResponse>>(All.Where(s => s.UserId == userId)
            .Select(s => new SessionResponse(s.Id, s.ClientType, s.DeviceId, s.CreatedAtUtc, s.LastSeenAtUtc, s.ExpiresAtUtc, s.RevokedAtUtc, s.RevokedReason))
            .ToList());
}

internal sealed class FakePersistence : IIdentityPersistence
{
    public Result NextSave { get; set; } = Result.Success();
    public int Saves { get; private set; }

    public Task<Result> SaveChangesAsync(CancellationToken ct)
    {
        Saves++;
        return Task.FromResult(NextSave);
    }
}

internal sealed class FakeOrganization : IBranchDirectory, IWarehouseDirectory
{
    public Dictionary<Guid, Guid> WarehouseBranch { get; } = []; // warehouse -> branch

    public (Guid Branch, Guid Warehouse) AddWarehouse(Guid? branchId = null)
    {
        var warehouse = Guid.NewGuid();
        var branch = branchId ?? Guid.NewGuid();
        WarehouseBranch[warehouse] = branch;
        return (branch, warehouse);
    }

    Task<IReadOnlyList<BranchInfo>> IBranchDirectory.FindManyAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BranchInfo>>(branchIds.Distinct().Where(WarehouseBranch.ContainsValue)
            .Select(id => new BranchInfo(id, Guid.Empty, "B", "فرع", true)).ToList());

    public Task<WarehouseInfo?> FindAsync(Guid warehouseId, CancellationToken ct) => throw new NotSupportedException();

    Task<IReadOnlyList<WarehouseInfo>> IWarehouseDirectory.FindManyAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<WarehouseInfo>>(warehouseIds.Distinct().Where(WarehouseBranch.ContainsKey)
            .Select(id => new WarehouseInfo(id, WarehouseBranch[id], "W", WarehouseType.Physical, true, true)).ToList());

    public Task<IReadOnlyList<Guid>> ListIdsInBranchesAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(WarehouseBranch.Where(w => branchIds.Contains(w.Value)).Select(w => w.Key).ToList());
}
