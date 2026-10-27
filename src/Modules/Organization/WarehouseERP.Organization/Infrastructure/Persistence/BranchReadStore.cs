using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

internal sealed class BranchReadStore(DbContext db) : IBranchReadStore
{
    public async Task<BranchDetailsResponse?> GetAsync(Guid branchId, CancellationToken ct)
    {
        var row = await db.Set<Branch>().AsNoTracking()
            .Where(x => x.Id == branchId)
            .Select(x => new { x.Id, x.CompanyId, x.Code, x.NameAr, x.NameEn, x.IsActive, x.RowVersion })
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new BranchDetailsResponse(row.Id, row.CompanyId, row.Code, row.NameAr, row.NameEn, row.IsActive,
                Convert.ToBase64String(row.RowVersion));
    }

    public async Task<PagedResult<BranchSummaryResponse>> ListAsync(
        BranchListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Branch>().AsNoTracking();
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search)));
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.ThroughWarehouseIds is { } scope)
        {
            var ids = scope.ToArray(); // a parameter collection EF Core translates
            query = query.Where(x => x.Warehouses.Any(w => ids.Contains(w.Id)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new BranchSummaryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<BranchSummaryResponse>(items, page, pageSize, total);
    }

    public async Task<bool> HoldsAnyWarehouseAsync(Guid branchId, IReadOnlySet<Guid> warehouseIds, CancellationToken ct)
    {
        var ids = warehouseIds.ToArray();
        return ids.Length > 0
               && await db.Set<Warehouse>().AnyAsync(x => x.BranchId == branchId && ids.Contains(x.Id), ct);
    }
}
