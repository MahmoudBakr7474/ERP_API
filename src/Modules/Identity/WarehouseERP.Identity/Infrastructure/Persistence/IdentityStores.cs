using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Identity.Application;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

internal sealed class UserAccessRepository(DbContext db) : IUserAccessRepository
{
    public Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct) =>
        db.Set<UserAccess>()
            .Include(x => x.Branches)
            .Include(x => x.Warehouses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);

    public void Add(UserAccess access) => db.Set<UserAccess>().Add(access);
}

internal sealed class SessionRepository(DbContext db, TimeProvider clock) : ISessionRepository
{
    private const int ListLimit = 50;

    public void Add(UserSession session) => db.Set<UserSession>().Add(session);

    public Task<UserSession?> GetAsync(Guid sessionId, CancellationToken ct) =>
        db.Set<UserSession>().SingleOrDefaultAsync(s => s.Id == sessionId, ct);

    public Task<UserSession?> FindByRefreshHashAsync(byte[] refreshHash, CancellationToken ct) =>
        db.Set<UserSession>().FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshHash || s.PreviousRefreshTokenHash == refreshHash, ct);

    public async Task<IReadOnlyList<UserSession>> GetActiveAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.Set<UserSession>()
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SessionItem>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Set<UserSession>().AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastSeenAtUtc).ThenBy(s => s.Id)
            .Take(ListLimit)
            .Select(s => new SessionItem(
                s.Id, s.ClientType, s.DeviceId, s.CreatedAtUtc, s.LastSeenAtUtc, s.ExpiresAtUtc, s.RevokedAtUtc, s.RevokedReason))
            .ToListAsync(ct);
}

internal sealed class UserReadStore(DbContext db, TimeProvider clock) : IUserReadStore
{
    public async Task<UserDetails?> GetAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var user = await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSummary(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .SingleOrDefaultAsync(ct);
        if (user is null)
            return null;

        var roles = await db.Set<IdentityUserRole<Guid>>()
            .Where(x => x.UserId == userId)
            .Join(db.Set<ApplicationRole>(), x => x.RoleId, r => r.Id, (x, r) => r)
            .OrderBy(r => r.Name)
            .Select(r => new NamedItem(r.Id, r.Name!))
            .ToListAsync(ct);
        var access = await db.Set<UserAccess>().AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new UserAccessDetails(
                a.IsCompanyWide,
                a.Branches.Select(b => b.BranchId).ToList(),
                a.Warehouses.Select(w => w.WarehouseId).ToList()))
            .SingleOrDefaultAsync(ct);
        return new UserDetails(
            user.Id, user.UserName, user.DisplayName, user.Email, user.IsActive, user.IsLockedOut, roles,
            access ?? new UserAccessDetails(false, [], []));
    }

    public async Task<PagedResult<UserSummary>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
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
            .Select(u => new UserSummary(u.Id, u.UserName!, u.DisplayName, u.Email, u.IsActive, u.LockoutEnd != null && u.LockoutEnd > now))
            .ToListAsync(ct);
        return new PagedResult<UserSummary>(items, page, pageSize, total);
    }
}

internal sealed class UserDirectory(DbContext db) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string>();
        var ids = userIds.Distinct().ToArray();
        return await db.Set<ApplicationUser>().AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
    }
}

internal sealed class IdentityPersistence(DbContext db) : IIdentityPersistence
{
    // Unique indexes whose violation is an expected outcome: ASP.NET Core Identity's UserNameIndex and RoleNameIndex
    // (two requests creating the same name at once), and one UserAccess per user (two first-time scope edits).
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UserNameIndex"] = IdentityApplicationErrors.UserNameTaken,
        ["RoleNameIndex"] = IdentityApplicationErrors.RoleNameTaken,
        ["IX_UserAccess_UserId"] = CommonErrors.ConcurrencyConflict,
    };

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
