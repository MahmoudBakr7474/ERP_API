using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class ProductReadStore(DbContext db) : IProductReadStore
{
    public async Task<ProductDetailsResponse?> GetAsync(Guid productId, CancellationToken ct)
    {
        var row = await db.Set<Product>().AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new
            {
                x.Id, x.SKU, x.NameAr, x.NameEn, x.CategoryId, x.BaseUnitId, x.ProductType, x.TrackingMode, x.IsActive, x.RowVersion,
                Units = x.Units.OrderBy(u => u.ConversionFactor)
                    .Select(u => new ProductUnitResponse(u.Id, u.UnitId, u.ConversionFactor, u.IsActive)).ToList(),
            })
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new ProductDetailsResponse(row.Id, row.SKU, row.NameAr, row.NameEn, row.CategoryId, row.BaseUnitId, row.ProductType,
                row.TrackingMode, row.IsActive, Convert.ToBase64String(row.RowVersion), row.Units);
    }

    public async Task<PagedResult<ProductSummaryResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Product>().AsNoTracking();
        if (search is not null)
            query = query.Where(x => x.SKU.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search)));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.SKU)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductSummaryResponse(x.Id, x.SKU, x.NameAr, x.NameEn, x.ProductType, x.TrackingMode, x.IsActive))
            .ToListAsync(ct);
        return new PagedResult<ProductSummaryResponse>(items, page, pageSize, total);
    }
}
