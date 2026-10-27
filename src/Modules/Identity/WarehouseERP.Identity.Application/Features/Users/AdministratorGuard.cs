using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users;

// Someone must always be able to manage every user: the last active Administrator can't lose the role or be
// deactivated, and neither can the last active company-wide one, nor lose that scope. A branch-scoped Administrator
// can't manage company-wide users or grant company-wide scope, so nobody could bring that scope back.
internal static class AdministratorGuard
{
    /// <summary>
    /// For a user about to lose the Administrator role or be deactivated: succeeds when they are not an Administrator,
    /// or when another active one remains, and another active company-wide one if they are company-wide. The
    /// Administrator role then also gets a new version in the coming save: two removals checked at the same time (A
    /// removes B while B removes A) can't both commit, the second answers Common.ConcurrencyConflict.
    /// </summary>
    public static async Task<Result> EnsureAnotherAdministratorAsync(
        IRoleRepository roles, IUserRoleAssignments roleAssignments, IUserAccessRepository access, Guid userId, CancellationToken ct)
    {
        if (await AdministratorRoleHeldByAsync(roles, roleAssignments, userId, ct) is not { } adminRoleId)
            return Result.Success();
        if (await roleAssignments.CountActiveUsersInRoleAsync(adminRoleId, excludingUserId: userId, ct) == 0)
            return UserErrors.LastAdministrator;
        return (await access.GetAsync(userId, ct))?.IsCompanyWide == true
            ? await EnsureAnotherCompanyWideAsync(roleAssignments, adminRoleId, userId, ct)
            : Result.Success();
    }

    /// <summary>For a user about to lose company-wide scope: as above, when they are a company-wide Administrator.</summary>
    public static async Task<Result> EnsureAnotherCompanyWideAdministratorAsync(
        IRoleRepository roles, IUserRoleAssignments roleAssignments, Guid userId, CancellationToken ct) =>
        await AdministratorRoleHeldByAsync(roles, roleAssignments, userId, ct) is { } adminRoleId
            ? await EnsureAnotherCompanyWideAsync(roleAssignments, adminRoleId, userId, ct)
            : Result.Success();

    // The Administrator role's id when the user holds it. Its new version is read here, before any count: a removal
    // that commits after this read then fails the coming save, and one that committed before it is counted. Only
    // changes to an Administrator touch the role (it keeps temporal history, which shouldn't fill with empty versions).
    private static async Task<Guid?> AdministratorRoleHeldByAsync(
        IRoleRepository roles, IUserRoleAssignments roleAssignments, Guid userId, CancellationToken ct)
    {
        if (await roles.FindAdministratorIdAsync(ct) is not { } adminRoleId)
            return null;
        if (!(await roleAssignments.GetRoleIdsAsync(userId, ct)).Contains(adminRoleId))
            return null;
        await roles.TouchAsync(adminRoleId, ct);
        return adminRoleId;
    }

    private static async Task<Result> EnsureAnotherCompanyWideAsync(
        IUserRoleAssignments roleAssignments, Guid adminRoleId, Guid userId, CancellationToken ct) =>
        await roleAssignments.CountActiveCompanyWideUsersInRoleAsync(adminRoleId, excludingUserId: userId, ct) == 0
            ? UserErrors.LastAdministrator
            : Result.Success();
}
