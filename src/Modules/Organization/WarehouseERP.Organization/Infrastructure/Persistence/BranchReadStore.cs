using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Contracts;
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
        // A parameter collection EF Core translates; a scoped caller counts and reaches only these warehouses.
        var ids = filter.ThroughWarehouseIds?.ToArray();
        if (ids is not null)
            query = query.Where(x => x.Warehouses.Any(w => ids.Contains(w.Id)));
        // Scoped callers only reach branches holding one of their warehouses, so "without warehouses" is empty for them.
        if (filter.HasWarehouses is { } has)
            query = has ? query.Where(x => x.Warehouses.Any()) : query.Where(x => !x.Warehouses.Any());

        var total = await query.CountAsync(ct);
        var paged = Sorted(query, filter.Sort, filter.Descending).Skip((page - 1) * pageSize).Take(pageSize);
        var items = ids is null
            ? await paged.Select(x => new BranchSummaryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.IsActive,
                    x.Warehouses.Count(), x.Warehouses.Count(w => w.IsActive)))
                .ToListAsync(ct)
            : await paged.Select(x => new BranchSummaryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.IsActive,
                    x.Warehouses.Count(w => ids.Contains(w.Id)), x.Warehouses.Count(w => w.IsActive && ids.Contains(w.Id))))
                .ToListAsync(ct);
        return new PagedResult<BranchSummaryResponse>(items, page, pageSize, total);
    }

    // Ties by code then id, so pages never overlap.
    private static IOrderedQueryable<Branch> Sorted(IQueryable<Branch> query, OrganizationSort sort, bool descending) =>
        (sort, descending) switch
        {
            (OrganizationSort.NameAr, false) => query.OrderBy(x => x.NameAr).ThenBy(x => x.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameAr, true) => query.OrderByDescending(x => x.NameAr).ThenBy(x => x.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameEn, false) => query.OrderBy(x => x.NameEn).ThenBy(x => x.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameEn, true) => query.OrderByDescending(x => x.NameEn).ThenBy(x => x.Code).ThenBy(x => x.Id),
            (_, true) => query.OrderByDescending(x => x.Code).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Code).ThenBy(x => x.Id),
        };

    public async Task<bool> HoldsAnyWarehouseAsync(Guid branchId, IReadOnlySet<Guid> warehouseIds, CancellationToken ct)
    {
        var ids = warehouseIds.ToArray();
        return ids.Length > 0
               && await db.Set<Warehouse>().AnyAsync(x => x.BranchId == branchId && ids.Contains(x.Id), ct);
    }
}
