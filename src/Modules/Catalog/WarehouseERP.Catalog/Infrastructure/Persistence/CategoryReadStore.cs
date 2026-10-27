using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Categories;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class CategoryReadStore(DbContext db) : ICategoryReadStore
{
    public async Task<CategoryDetailsResponse?> GetAsync(Guid categoryId, CancellationToken ct)
    {
        var row = await db.Set<Category>().AsNoTracking()
            .Where(x => x.Id == categoryId)
            .Select(x => new { x.Id, x.ParentId, x.Code, x.NameAr, x.NameEn, x.IsActive, x.RowVersion })
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new CategoryDetailsResponse(row.Id, row.ParentId, row.Code, row.NameAr, row.NameEn, row.IsActive,
                Convert.ToBase64String(row.RowVersion));
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(bool? isActive, CancellationToken ct)
    {
        var query = db.Set<Category>().AsNoTracking();
        if (isActive is { } active)
            query = query.Where(x => x.IsActive == active);
        return await query
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Select(x => new CategoryResponse(x.Id, x.ParentId, x.Code, x.NameAr, x.NameEn, x.IsActive))
            .ToListAsync(ct);
    }
}
