using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application;

// Commands that keep cross-row invariants. Production API must also validate authorization,
// input and uniqueness errors. The DbContext is the shared ErpDbContext; this module touches
// only its own tables through Set<T>().
public sealed class ProductCommands(DbContext db)
{
    public async Task<Guid> CreateProductAsync(
        string sku, string nameAr, Guid baseUnitId, Guid? categoryId,
        ProductType type, TrackingMode tracking, Guid? actorId, CancellationToken ct)
    {
        if (type != ProductType.StockItem && tracking != TrackingMode.None)
            throw new ArgumentException("Only stock items can use batch/serial tracking.");
        if (!await db.Set<Unit>().AnyAsync(u => u.Id == baseUnitId && u.IsActive, ct))
            throw new InvalidOperationException("Base unit does not exist or is inactive.");
        var product = new Product
        {
            SKU = sku.Trim(), NameAr = nameAr.Trim(), CategoryId = categoryId,
            BaseUnitId = baseUnitId, ProductType = type,
            TrackingMode = tracking, CreatedByUserId = actorId
        };
        product.Units.Add(new ProductUnit
        {
            UnitId = baseUnitId, ConversionFactor = 1m, CreatedByUserId = actorId
        });
        // Product.BaseUnitId alone stores base-unit truth. This row is still required.
        db.Set<Product>().Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }
}
