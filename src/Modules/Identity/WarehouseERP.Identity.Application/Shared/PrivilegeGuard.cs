using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Shared;

// No privilege escalation through Identity itself: nobody hands out permissions or scope they don't hold, or takes
// over (resets, edits, re-roles) an account that holds more than they do. Without this, identity.users.manage alone
// would be enough to become Administrator. Revoking sessions is exempt: it never grants anything.
internal static class PrivilegeGuard
{
    /// <summary>The existing roles among the ids; any unknown id fails.</summary>
    public static async Task<Result<IReadOnlyList<Role>>> GetRolesAsync(
        IRoleRepository roles, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        var wanted = roleIds.Distinct().ToList();
        if (wanted.Count == 0)
            return Result.Success<IReadOnlyList<Role>>([]);
        var found = await roles.GetManyAsync(wanted, ct);
        return found.Count == wanted.Count ? Result.Success(found) : IdentityApplicationErrors.UnknownRoles;
    }

    public static bool HoldsAll(ICurrentUser caller, IEnumerable<string> permissions) => permissions.All(caller.HasPermission);

    public static bool HoldsAll(ICurrentUser caller, IEnumerable<Role> roles, IEnumerable<PermissionDefinition> catalog) =>
        roles.All(role => HoldsAll(caller, role.GrantsAllPermissions ? catalog.Select(p => p.Name) : role.Permissions));

    /// <summary>The caller holds every permission the target user's roles grant.</summary>
    public static async Task<Result> EnsureCanManageAsync(
        ICurrentUser caller, IUserAccounts accounts, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
        Guid targetUserId, CancellationToken ct)
    {
        var targetRoles = await roles.GetManyAsync(await accounts.GetRoleIdsAsync(targetUserId, ct), ct);
        return HoldsAll(caller, targetRoles, catalog) ? Result.Success() : IdentityApplicationErrors.ExceedsOwnPermissions;
    }

    /// <summary>
    /// The ids exist (through Organization's contracts; the foreign keys stay the final guard) and lie within the
    /// caller's own scope: company-wide only from a company-wide caller, a branch only from a holder of that branch.
    /// </summary>
    public static async Task<Result> EnsureGrantableScopeAsync(
        ICurrentUser caller, IUserAccessRepository access, IBranchDirectory branches, IWarehouseDirectory warehouses,
        bool companyWide, IReadOnlyCollection<Guid> branchIds, IReadOnlyCollection<Guid> warehouseIds, CancellationToken ct)
    {
        if (companyWide)
            return caller.HasAllWarehouses ? Result.Success() : IdentityApplicationErrors.ExceedsOwnScope;

        var wantedBranches = branchIds.Distinct().ToList();
        if (wantedBranches.Count > 0 && (await branches.FindManyAsync(wantedBranches, ct)).Count != wantedBranches.Count)
            return IdentityApplicationErrors.UnknownBranches;
        var wantedWarehouses = warehouseIds.Distinct().ToList();
        if (wantedWarehouses.Count > 0 && (await warehouses.FindManyAsync(wantedWarehouses, ct)).Count != wantedWarehouses.Count)
            return IdentityApplicationErrors.UnknownWarehouses;

        if (caller.HasAllWarehouses)
            return Result.Success();
        if (!wantedWarehouses.TrueForAll(caller.CanAccessWarehouse))
            return IdentityApplicationErrors.ExceedsOwnScope;
        if (wantedBranches.Count == 0)
            return Result.Success();
        var own = caller.UserId is { } callerId ? await access.GetAsync(callerId, ct) : null;
        var ownBranches = own?.Branches.Select(g => g.BranchId).ToHashSet() ?? [];
        return wantedBranches.TrueForAll(ownBranches.Contains) ? Result.Success() : IdentityApplicationErrors.ExceedsOwnScope;
    }
}
