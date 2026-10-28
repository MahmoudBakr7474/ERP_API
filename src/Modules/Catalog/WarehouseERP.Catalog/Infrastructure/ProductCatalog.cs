using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure;

// What other modules may learn about a product: a projection, never the entity.
internal sealed class ProductCatalog(DbContext db) : IProductCatalog
{
    // Filters run on the entity, before the projection: EF Core can't translate a filter on a constructed record.
    private static readonly Expression<Func<Product, ProductInfo>> ToInfo =
        x => new ProductInfo(x.Id, x.SKU, x.NameAr, x.BaseUnitId, x.ProductType, x.TrackingMode, x.IsActive);

    private static readonly Expression<Func<ProductUnit, ProductUnitInfo>> ToUnitInfo =
        x => new ProductUnitInfo(
            x.Id, x.ProductId, x.UnitId, x.Unit.Code, x.Unit.NameAr, x.ConversionFactor, x.UnitId == x.Product.BaseUnitId,
            x.Unit.AllowsFraction, x.Product.BaseUnit.AllowsFraction, x.IsActive && x.Unit.IsActive);

    public Task<ProductInfo?> FindAsync(Guid productId, CancellationToken ct) =>
        db.Set<Product>().AsNoTracking().Where(x => x.Id == productId).Select(ToInfo).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProductInfo>> FindManyAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<Product>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToInfo).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductUnitInfo>> FindUnitsAsync(IReadOnlyCollection<Guid> productUnitIds, CancellationToken ct)
    {
        var ids = productUnitIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<ProductUnit>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToUnitInfo).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductUnitInfo>> FindBaseUnitsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<ProductUnit>().AsNoTracking()
                .Where(x => ids.Contains(x.ProductId) && x.UnitId == x.Product.BaseUnitId)
                .Select(ToUnitInfo)
                .ToListAsync(ct);
    }
}
