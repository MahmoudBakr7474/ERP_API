using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// IUserRoleAssignments straight on identity.UserRoles, by role id (UserManager only knows roles by name). Tracked
// only; the use case saves.
internal sealed class UserRoleAssignments(DbContext db) : IUserRoleAssignments
{
    public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken ct) =>
        await db.Set<IdentityUserRole<Guid>>().Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync(ct);

    public async Task<Result> SetRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        // Finds a user added earlier in this use case too (CreateUser), not only stored ones.
        if (await db.Set<ApplicationUser>().FindAsync([userId], ct) is null)
            return UserErrors.NotFound;
        var wanted = roleIds.ToHashSet();
        var assignments = db.Set<IdentityUserRole<Guid>>();
        var current = await assignments.Where(x => x.UserId == userId).ToListAsync(ct);
        assignments.RemoveRange(current.Where(x => !wanted.Contains(x.RoleId)));
        foreach (var roleId in wanted.Except(current.Select(x => x.RoleId)))
            assignments.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        return Result.Success();
    }

    public Task<int> CountActiveUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct) =>
        ActiveHolders(roleId, excludingUserId).CountAsync(ct);

    public Task<int> CountActiveCompanyWideUsersInRoleAsync(Guid roleId, Guid? excludingUserId, CancellationToken ct) =>
        ActiveHolders(roleId, excludingUserId)
            .Where(userId => db.Set<UserAccess>().Any(a => a.UserId == userId && a.IsCompanyWide))
            .CountAsync(ct);

    private IQueryable<Guid> ActiveHolders(Guid roleId, Guid? excludingUserId) =>
        db.Set<IdentityUserRole<Guid>>()
            .Where(x => x.RoleId == roleId && x.UserId != excludingUserId)
            .Join(db.Set<ApplicationUser>().Where(u => u.IsActive), x => x.UserId, u => u.Id, (x, u) => u.Id);

    public Task<bool> AnyUserInRoleAsync(Guid roleId, CancellationToken ct) =>
        db.Set<IdentityUserRole<Guid>>().AnyAsync(x => x.RoleId == roleId, ct);
}
