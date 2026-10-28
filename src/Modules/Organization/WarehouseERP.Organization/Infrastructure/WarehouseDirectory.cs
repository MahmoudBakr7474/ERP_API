using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// What other modules may learn about warehouses and branches: projections, never the entities.
internal sealed class WarehouseDirectory(DbContext db) : IWarehouseDirectory, IBranchDirectory, IWarehouseListing
{
    // Filters run on the entity, before the projection: EF Core can't translate a filter on a constructed record.
    private static readonly Expression<Func<Warehouse, WarehouseInfo>> ToInfo =
        x => new WarehouseInfo(x.Id, x.BranchId, x.Code, x.WarehouseType, x.AllowsSelling, x.IsActive);

    public Task<WarehouseInfo?> FindAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().AsNoTracking().Where(x => x.Id == warehouseId).Select(ToInfo).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<WarehouseInfo>> FindManyAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken ct)
    {
        var ids = warehouseIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<Warehouse>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToInfo).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WarehouseListItem>> ListActiveAsync(WarehouseType type, CancellationToken ct) =>
        await (from warehouse in db.Set<Warehouse>().AsNoTracking()
               join branch in db.Set<Branch>().AsNoTracking() on warehouse.BranchId equals branch.Id
               where warehouse.IsActive && warehouse.WarehouseType == type && branch.IsActive
               orderby branch.Code, warehouse.Code
               select new WarehouseListItem(warehouse.Id, branch.Id, branch.Code, warehouse.Code, warehouse.NameAr, warehouse.NameEn))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListIdsInBranchesAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct)
    {
        var ids = branchIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<Warehouse>().AsNoTracking().Where(x => ids.Contains(x.BranchId)).Select(x => x.Id).ToListAsync(ct);
    }

    async Task<IReadOnlyList<BranchInfo>> IBranchDirectory.FindManyAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct)
    {
        var ids = branchIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<Branch>().AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new BranchInfo(x.Id, x.CompanyId, x.Code, x.NameAr, x.IsActive))
                .ToListAsync(ct);
    }
}
