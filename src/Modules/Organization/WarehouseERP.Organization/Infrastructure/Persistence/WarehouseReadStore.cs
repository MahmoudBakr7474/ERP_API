using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class WarehouseReadStore(DbContext db) : IWarehouseReadStore
{
    public Task<WarehouseDetails?> GetAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().AsNoTracking()
            .Where(x => x.Id == warehouseId)
            .Select(x => new WarehouseDetails(
                x.Id, x.BranchId, x.Code, x.NameAr, x.NameEn, x.WarehouseType, x.AllowsSelling, x.IsActive,
                x.Locations
                    .OrderByDescending(l => l.IsDefault).ThenBy(l => l.Code)
                    .Select(l => new WarehouseLocationItem(l.Id, l.Code, l.NameAr, l.NameEn, l.IsDefault, l.IsActive))
                    .ToList()))
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<WarehouseSummary>> ListAsync(
        WarehouseListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Warehouse>().AsNoTracking();
        if (filter.BranchId is { } branchId)
            query = query.Where(x => x.BranchId == branchId);
        if (filter.OnlyWarehouseIds is { } scope)
        {
            var ids = scope.ToArray(); // a parameter collection EF Core translates
            query = query.Where(x => ids.Contains(x.Id));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new WarehouseSummary(
                x.Id, x.BranchId, x.Code, x.NameAr, x.NameEn, x.WarehouseType, x.AllowsSelling, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<WarehouseSummary>(items, page, pageSize, total);
    }
}
