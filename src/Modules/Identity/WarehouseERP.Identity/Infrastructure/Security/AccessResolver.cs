using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Identity.Infrastructure.Security;

/// <param name="AllPermissions">The Administrator role: every permission, present and future.</param>
/// <param name="WarehouseIds">Effective warehouses; ignored when <paramref name="AllWarehouses"/> is true.</param>
internal sealed record EffectiveAccess(
    bool AllPermissions, IReadOnlyCollection<string> Permissions, bool AllWarehouses, IReadOnlyCollection<Guid> WarehouseIds);

// What a user may do and where, read fresh on every request: a role, permission or scope change applies to the next
// request without signing in again. Branch grants expand to the branch's current warehouses through Organization's
// contract, so a warehouse added to the branch later is covered at once.
internal sealed class AccessResolver(DbContext db, ILookupNormalizer normalizer, IWarehouseDirectory warehouses)
{
    public async Task<EffectiveAccess> ResolveAsync(Guid userId, CancellationToken ct)
    {
        var administrator = normalizer.NormalizeName(Role.AdministratorName);
        var roleIds = db.Set<IdentityUserRole<Guid>>().Where(x => x.UserId == userId).Select(x => x.RoleId);
        var isAdministrator = await db.Set<ApplicationRole>()
            .AnyAsync(r => roleIds.Contains(r.Id) && r.IsSystem && r.NormalizedName == administrator, ct);
        IReadOnlyCollection<string> permissions = isAdministrator
            ? []
            : await db.Set<IdentityRoleClaim<Guid>>()
                .Where(c => roleIds.Contains(c.RoleId) && c.ClaimType == ErpClaimTypes.Permission)
                .Select(c => c.ClaimValue!)
                .Distinct()
                .ToListAsync(ct);

        var access = await db.Set<UserAccess>().AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new
            {
                a.IsCompanyWide,
                Branches = a.Branches.Select(b => b.BranchId).ToList(),
                Warehouses = a.Warehouses.Select(w => w.WarehouseId).ToList(),
            })
            .SingleOrDefaultAsync(ct);
        if (access is null)
            return new EffectiveAccess(isAdministrator, permissions, AllWarehouses: false, []);
        if (access.IsCompanyWide)
            return new EffectiveAccess(isAdministrator, permissions, AllWarehouses: true, []);

        var warehouseIds = access.Warehouses.ToHashSet();
        if (access.Branches.Count > 0)
            warehouseIds.UnionWith(await warehouses.ListIdsInBranchesAsync(access.Branches, ct));
        return new EffectiveAccess(isAdministrator, permissions, AllWarehouses: false, warehouseIds);
    }
}
