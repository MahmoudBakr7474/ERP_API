using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Contracts.UserAccess;
using WarehouseERP.Identity.Application.Contracts.Users;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class UserReadStore(DbContext db, TimeProvider clock) : IUserReadStore
{
    public async Task<UserDetailsResponse?> GetAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var user = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSummaryResponse(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .SingleOrDefaultAsync(ct);
        if (user is null)
            return null;

        var roles = await db.Set<IdentityUserRole<Guid>>()
            .Where(x => x.UserId == userId)
            .Join(db.Set<ApplicationRole>(), x => x.RoleId, r => r.Id, (x, r) => r)
            .OrderBy(r => r.Name)
            .Select(r => new UserRoleResponse(r.Id, r.Name!))
            .ToListAsync(ct);
        var access = await db.Set<UserAccess>().AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new UserAccessResponse(
                a.IsCompanyWide,
                a.Branches.Select(b => b.BranchId).ToList(),
                a.Warehouses.Select(w => w.WarehouseId).ToList(),
                a.Revision))
            .SingleOrDefaultAsync(ct);
        return new UserDetailsResponse(
            user.Id, user.UserName, user.DisplayName, user.Email, user.IsActive, user.IsLockedOut, roles,
            access ?? new UserAccessResponse(false, [], [], 0));
    }

    public async Task<PagedResult<UserSummaryResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var query = db.Set<ApplicationUser>().AsNoTracking();
        if (search is not null)
            query = query.Where(u => u.UserName!.Contains(search) || u.DisplayName.Contains(search)
                                                                  || (u.Email != null && u.Email.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.UserName).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserSummaryResponse(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .ToListAsync(ct);
        return new PagedResult<UserSummaryResponse>(items, page, pageSize, total);
    }
}
