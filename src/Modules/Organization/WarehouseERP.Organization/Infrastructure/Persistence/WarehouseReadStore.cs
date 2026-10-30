using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure.Persistence;

// Row versions leave as base64: EF Core runs that conversion on the materialized rows, after SQL.
internal sealed class WarehouseReadStore(DbContext db) : IWarehouseReadStore
{
    private static readonly Expression<Func<WarehouseLocation, WarehouseLocationResponse>> ToLocationResponse =
        l => new WarehouseLocationResponse(l.Id, l.Code, l.NameAr, l.NameEn, l.IsDefault, l.IsActive, Convert.ToBase64String(l.RowVersion));

    public Task<WarehouseDetailsResponse?> GetAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().AsNoTracking()
            .Where(x => x.Id == warehouseId)
            .Select(x => new WarehouseDetailsResponse(
                x.Id, x.BranchId, x.Code, x.NameAr, x.NameEn, x.WarehouseType, x.AllowsSelling, x.IsActive,
                Convert.ToBase64String(x.RowVersion),
                x.Locations
                    .OrderByDescending(l => l.IsDefault).ThenBy(l => l.Code)
                    .Select(l => new WarehouseLocationResponse(
                        l.Id, l.Code, l.NameAr, l.NameEn, l.IsDefault, l.IsActive, Convert.ToBase64String(l.RowVersion)))
                    .ToList()))
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<WarehouseSummaryResponse>> ListAsync(
        WarehouseListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = Filtered(db.Set<Warehouse>().AsNoTracking(), filter);
        var total = await query.CountAsync(ct);
        var items = await Sorted(query, filter.Sort, filter.Descending)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToSummary)
            .ToListAsync(ct);
        return new PagedResult<WarehouseSummaryResponse>(items, page, pageSize, total);
    }

    internal static readonly Expression<Func<Warehouse, WarehouseSummaryResponse>> ToSummary =
        x => new WarehouseSummaryResponse(
            x.Id, x.BranchId, x.Code, x.NameAr, x.NameEn, x.WarehouseType, x.AllowsSelling, x.IsActive,
            x.Branch.Code, x.Branch.NameAr, x.Branch.NameEn, x.Branch.IsActive,
            x.Locations.Count(), x.Locations.Count(l => l.IsActive),
            x.Locations.Where(l => l.IsDefault).Select(l => l.Code).FirstOrDefault(),
            x.Locations.Where(l => l.IsDefault).Select(l => (bool?)l.IsActive).FirstOrDefault());

    internal static IQueryable<Warehouse> Filtered(IQueryable<Warehouse> query, WarehouseListFilter filter)
    {
        if (filter.BranchId is { } branchId)
            query = query.Where(x => x.BranchId == branchId);
        if (filter.OnlyWarehouseIds is { } scope)
        {
            var ids = scope.ToArray(); // a parameter collection EF Core translates
            query = query.Where(x => ids.Contains(x.Id));
        }
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search)));
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);
        if (filter.Type is { } type)
            query = query.Where(x => x.WarehouseType == type);
        if (filter.AllowsSelling is { } sells)
            query = query.Where(x => x.AllowsSelling == sells);
        return query;
    }

    // Warehouse codes repeat across branches: ties by branch code, then id, so pages never overlap.
    internal static IOrderedQueryable<Warehouse> Sorted(IQueryable<Warehouse> query, OrganizationSort sort, bool descending) =>
        (sort, descending) switch
        {
            (OrganizationSort.NameAr, false) => query.OrderBy(x => x.NameAr).ThenBy(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameAr, true) => query.OrderByDescending(x => x.NameAr).ThenBy(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameEn, false) => query.OrderBy(x => x.NameEn).ThenBy(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
            (OrganizationSort.NameEn, true) => query.OrderByDescending(x => x.NameEn).ThenBy(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
            (_, true) => query.OrderByDescending(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Code).ThenBy(x => x.Branch.Code).ThenBy(x => x.Id),
        };

    public Task<bool> ExistsAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().AnyAsync(x => x.Id == warehouseId, ct);

    public Task<WarehouseLocationResponse?> GetLocationAsync(Guid warehouseId, Guid locationId, CancellationToken ct) =>
        db.Set<WarehouseLocation>().AsNoTracking()
            .Where(x => x.Id == locationId && x.WarehouseId == warehouseId)
            .Select(ToLocationResponse)
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<WarehouseLocationResponse>> ListLocationsAsync(
        Guid warehouseId, string? search, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<WarehouseLocation>().AsNoTracking().Where(x => x.WarehouseId == warehouseId);
        if (search is not null)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search)));
        if (isActive is { } active)
            query = query.Where(x => x.IsActive == active);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToLocationResponse)
            .ToListAsync(ct);
        return new PagedResult<WarehouseLocationResponse>(items, page, pageSize, total);
    }
}
