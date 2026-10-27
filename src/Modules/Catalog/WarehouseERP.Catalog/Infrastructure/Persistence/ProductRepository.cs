using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(DbContext db) : IProductRepository
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration 01.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Products_SKU"] = ProductErrors.SkuTaken,
        ["IX_ProductUnits_ProductId_UnitId"] = ProductUnitDomainErrors.UnitAlreadyAdded,
        ["IX_ProductBarcodes_Barcode"] = ProductBarcodeErrors.Taken,
    };

    public Task<bool> IsUnitActiveAsync(Guid unitId, CancellationToken ct) =>
        db.Set<Unit>().AnyAsync(x => x.Id == unitId && x.IsActive, ct);

    public Task<UnitState?> GetUnitStateAsync(Guid unitId, CancellationToken ct) =>
        db.Set<Unit>().AsNoTracking()
            .Where(x => x.Id == unitId)
            .Select(x => new UnitState(x.IsActive, x.AllowsFraction))
            .SingleOrDefaultAsync(ct);

    public Task<bool> IsCategoryActiveAsync(Guid categoryId, CancellationToken ct) =>
        db.Set<Category>().AnyAsync(x => x.Id == categoryId && x.IsActive, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) =>
        db.Set<Product>().AnyAsync(x => x.SKU == sku, ct);

    public Task<bool> BarcodeExistsAsync(string barcode, CancellationToken ct) =>
        db.Set<ProductBarcode>().AnyAsync(x => x.Barcode == barcode, ct);

    public Task<Result> AddAsync(Product product, CancellationToken ct)
    {
        db.Set<Product>().Add(product);
        return DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
    }

    public async Task<Product?> GetForUpdateAsync(Guid productId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var product = await db.Set<Product>().SingleOrDefaultAsync(x => x.Id == productId, ct);
        if (product is not null && expectedRowVersion is not null)
            // The UPDATE's WHERE uses the original value, so this makes it compare with the caller's version.
            db.Entry(product).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return product;
    }

    public Task<Product?> GetWithUnitsForUpdateAsync(Guid productId, CancellationToken ct) =>
        db.Set<Product>()
            .Include(x => x.Units).ThenInclude(x => x.Barcodes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == productId, ct);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
