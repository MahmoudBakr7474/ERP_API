using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class UnitReadStore(DbContext db) : IUnitReadStore
{
    public async Task<UnitDetailsResponse?> GetAsync(Guid unitId, CancellationToken ct)
    {
        var row = await db.Set<Unit>().AsNoTracking()
            .Where(x => x.Id == unitId)
            .Select(x => new { x.Id, x.Code, x.NameAr, x.NameEn, x.AllowsFraction, x.IsActive, x.RowVersion })
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new UnitDetailsResponse(row.Id, row.Code, row.NameAr, row.NameEn, row.AllowsFraction, row.IsActive,
                Convert.ToBase64String(row.RowVersion));
    }

    public async Task<PagedResult<UnitSummaryResponse>> ListAsync(UnitListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Unit>().AsNoTracking();
        if (filter.Search is { } search)
            query = query.Where(x => x.Code.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search)));
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new UnitSummaryResponse(x.Id, x.Code, x.NameAr, x.NameEn, x.AllowsFraction, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<UnitSummaryResponse>(items, page, pageSize, total);
    }
}
