using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application;

internal sealed class ProductCatalog(DbContext db) : IProductCatalog
{
    public Task<ProductInfo?> FindAsync(Guid productId, CancellationToken ct) =>
        db.Set<Product>().AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new ProductInfo(x.Id, x.SKU, x.NameAr, x.BaseUnitId, x.ProductType, x.TrackingMode, x.IsActive))
            .SingleOrDefaultAsync(ct);
}
