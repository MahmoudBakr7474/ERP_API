using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Contracts.Roles;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence;

// Maps the domain Role onto ASP.NET Core Identity's tables: identity.Roles for the role, identity.RoleClaims of
// type "permission" for its permissions. Writes are tracked only; the use case saves.
internal sealed class RoleRepository(DbContext db, ILookupNormalizer normalizer, IEnumerable<PermissionDefinition> catalog)
    : IRoleRepository
{
    private const string PermissionClaim = ErpClaimTypes.Permission;

    public async Task<Role?> GetAsync(Guid roleId, CancellationToken ct) => (await LoadAsync([roleId], ct)).SingleOrDefault();

    public async Task<IReadOnlyList<Role>> GetManyAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct) =>
        roleIds.Count == 0 ? [] : await LoadAsync(roleIds, ct);

    public Task<Guid?> FindAdministratorIdAsync(CancellationToken ct)
    {
        var administrator = normalizer.NormalizeName(Role.AdministratorName);
        return db.Set<ApplicationRole>()
            .Where(r => r.IsSystem && r.NormalizedName == administrator)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync(ct);
    }

    public Task<bool> NameExistsAsync(string name, Guid? excludingRoleId, CancellationToken ct)
    {
        var normalized = normalizer.NormalizeName(name.Trim());
        return db.Set<ApplicationRole>().AnyAsync(r => r.NormalizedName == normalized && r.Id != excludingRoleId, ct);
    }

    public Task<Guid> AddAsync(Role role, CancellationToken ct)
    {
        var entity = new ApplicationRole
        {
            Name = role.Name,
            NormalizedName = normalizer.NormalizeName(role.Name),
            Description = role.Description,
            IsSystem = role.IsSystem,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };
        db.Set<ApplicationRole>().Add(entity); // the key is generated here, before the save
        foreach (var permission in role.Permissions)
            db.Set<IdentityRoleClaim<Guid>>().Add(Claim(entity.Id, permission));
        return Task.FromResult(entity.Id);
    }

    public async Task UpdateAsync(Role role, CancellationToken ct)
    {
        var entity = await db.Set<ApplicationRole>().SingleAsync(r => r.Id == role.Id, ct);
        entity.Name = role.Name;
        entity.NormalizedName = normalizer.NormalizeName(role.Name);
        entity.Description = role.Description;
        // A new stamp marks the row changed even when only permissions changed, so the role's temporal history and
        // UpdatedBy record every permission change.
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        var claims = db.Set<IdentityRoleClaim<Guid>>();
        var current = await claims.Where(c => c.RoleId == role.Id && c.ClaimType == PermissionClaim).ToListAsync(ct);
        claims.RemoveRange(current.Where(c => !role.Permissions.Contains(c.ClaimValue!)));
        foreach (var permission in role.Permissions.Except(current.Select(c => c.ClaimValue!), StringComparer.Ordinal))
            claims.Add(Claim(role.Id, permission));
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken ct) =>
        db.Set<ApplicationRole>().Remove(await db.Set<ApplicationRole>().SingleAsync(r => r.Id == roleId, ct)); // claims cascade

    public async Task<IReadOnlyList<RoleSummaryResponse>> ListAsync(CancellationToken ct)
    {
        var administrator = normalizer.NormalizeName(Role.AdministratorName);
        var catalogSize = catalog.Count();
        return await db.Set<ApplicationRole>().AsNoTracking()
            .OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name)
            .Select(r => new RoleSummaryResponse(
                r.Id, r.Name!, r.Description, r.IsSystem,
                r.IsSystem && r.NormalizedName == administrator
                    ? catalogSize
                    : db.Set<IdentityRoleClaim<Guid>>().Count(c => c.RoleId == r.Id && c.ClaimType == PermissionClaim)))
            .ToListAsync(ct);
    }

    private async Task<List<Role>> LoadAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToArray();
        var roles = await db.Set<ApplicationRole>().AsNoTracking().Where(r => ids.Contains(r.Id)).ToListAsync(ct);
        var permissions = (await db.Set<IdentityRoleClaim<Guid>>().AsNoTracking()
                .Where(c => ids.Contains(c.RoleId) && c.ClaimType == PermissionClaim)
                .Select(c => new { c.RoleId, c.ClaimValue })
                .ToListAsync(ct))
            .ToLookup(c => c.RoleId, c => c.ClaimValue!);
        return roles.Select(r => Role.Restore(r.Id, r.Name!, r.Description, r.IsSystem, permissions[r.Id])).ToList();
    }

    private static IdentityRoleClaim<Guid> Claim(Guid roleId, string permission) =>
        new() { RoleId = roleId, ClaimType = PermissionClaim, ClaimValue = permission };
}
