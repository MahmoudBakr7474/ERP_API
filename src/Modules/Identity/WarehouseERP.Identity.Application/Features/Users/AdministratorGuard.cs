using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users;

// Someone must always be able to manage users: the last active Administrator can't lose the role or be deactivated.
internal static class AdministratorGuard
{
    public static async Task<Result> EnsureAnotherAdministratorAsync(
        IRoleRepository roles, IUserRoleAssignments roleAssignments, Guid userId, CancellationToken ct)
    {
        if (await roles.FindAdministratorIdAsync(ct) is not { } adminRoleId)
            return Result.Success();
        if (!(await roleAssignments.GetRoleIdsAsync(userId, ct)).Contains(adminRoleId))
            return Result.Success();
        return await roleAssignments.CountActiveUsersInRoleAsync(adminRoleId, excludingUserId: userId, ct) > 0
            ? Result.Success()
            : UserErrors.LastAdministrator;
    }
}
