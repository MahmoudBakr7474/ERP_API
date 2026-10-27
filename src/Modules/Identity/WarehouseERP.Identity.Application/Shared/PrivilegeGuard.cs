using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Shared;

// No privilege escalation through Identity itself: nobody hands out permissions or scope they don't hold, or takes
// over (resets, edits, re-roles, re-scopes) an account that holds more permissions or reaches further than they do.
// Without this, identity.users.manage alone would be enough to become Administrator, and a branch manager could take
// over a company-wide account. Revoking sessions is exempt: it never grants anything.
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
        return found.Count == wanted.Count ? Result.Success(found) : RoleErrors.UnknownRoles;
    }

    public static bool HoldsAll(ICurrentUser caller, IEnumerable<string> permissions) => permissions.All(caller.HasPermission);

    // A role granting every permission (Administrator) also grants the ones later modules add, so holding today's
    // catalog is not enough: only a holder of every permission ("*") hands it out or manages its holders.
    public static bool HoldsAll(ICurrentUser caller, IEnumerable<Role> roles, IEnumerable<PermissionDefinition> catalog) =>
        roles.All(role => role.GrantsAllPermissions
            ? caller.HasPermission(ErpClaimTypes.AllPermissions)
            : HoldsAll(caller, role.Permissions));

    /// <summary>
    /// The caller holds every permission the target user's roles grant, and the target's current scope lies within
    /// the caller's own (as if the caller were granting it now).
    /// </summary>
    public static async Task<Result> EnsureCanManageAsync(
        ICurrentUser caller, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access,
        IEnumerable<PermissionDefinition> catalog, Guid targetUserId, CancellationToken ct)
    {
        var targetRoles = await roles.GetManyAsync(await roleAssignments.GetRoleIdsAsync(targetUserId, ct), ct);
        if (!HoldsAll(caller, targetRoles, catalog))
            return PermissionErrors.ExceedsOwnPermissions;
        if (caller.HasAllWarehouses || await access.GetAsync(targetUserId, ct) is not { } target)
            return Result.Success(); // no access row: the user reaches nothing
        return await WithinOwnScopeAsync(caller, access, target.IsCompanyWide,
            target.Branches.Select(b => b.BranchId).ToList(), target.Warehouses.Select(w => w.WarehouseId).ToList(), ct)
            ? Result.Success()
            : UserAccessErrors.ExceedsOwnScope;
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
            return caller.HasAllWarehouses ? Result.Success() : UserAccessErrors.ExceedsOwnScope;

        var wantedBranches = branchIds.Distinct().ToList();
        if (wantedBranches.Count > 0 && (await branches.FindManyAsync(wantedBranches, ct)).Count != wantedBranches.Count)
            return UserAccessErrors.UnknownBranches;
        var wantedWarehouses = warehouseIds.Distinct().ToList();
        if (wantedWarehouses.Count > 0 && (await warehouses.FindManyAsync(wantedWarehouses, ct)).Count != wantedWarehouses.Count)
            return UserAccessErrors.UnknownWarehouses;

        return await WithinOwnScopeAsync(caller, access, companyWide: false, wantedBranches, wantedWarehouses, ct)
            ? Result.Success()
            : UserAccessErrors.ExceedsOwnScope;
    }

    private static async Task<bool> WithinOwnScopeAsync(
        ICurrentUser caller, IUserAccessRepository access, bool companyWide, List<Guid> branchIds, List<Guid> warehouseIds,
        CancellationToken ct)
    {
        if (caller.HasAllWarehouses)
            return true;
        if (companyWide || !warehouseIds.TrueForAll(caller.CanAccessWarehouse))
            return false;
        if (branchIds.Count == 0)
            return true;
        var own = caller.UserId is { } callerId ? await access.GetAsync(callerId, ct) : null;
        var ownBranches = own?.Branches.Select(g => g.BranchId).ToHashSet() ?? [];
        return branchIds.TrueForAll(ownBranches.Contains);
    }
}
