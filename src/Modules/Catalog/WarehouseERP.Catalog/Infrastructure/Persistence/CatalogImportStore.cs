using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

// The catalog import's unit of work. Units and categories are few and loaded whole; products and barcodes only as
// the file names them, in chunks so a large file stays within SQL Server's parameter limit. Everything is saved by one
// SaveChanges (one transaction): unique keys taken by someone else meanwhile, or a product changed since it was
// loaded, roll the whole import back with the usual business errors.
internal sealed class CatalogImportStore(DbContext db) : ICatalogImportStore
{
    private const int ChunkSize = 1_000;

    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["IX_Units_Code"] = UnitErrors.CodeTaken,
        ["IX_Categories_Code"] = CategoryErrors.CodeTaken,
        ["IX_Products_SKU"] = ProductErrors.SkuTaken,
        ["IX_ProductUnits_ProductId_UnitId"] = ProductUnitDomainErrors.UnitAlreadyAdded,
        ["IX_ProductBarcodes_Barcode"] = ProductBarcodeErrors.Taken,
    };

    public async Task<CatalogImportSnapshot> LoadAsync(
        IReadOnlyCollection<string> skus, IReadOnlyCollection<string> barcodes, CancellationToken ct)
    {
        var units = await db.Set<Unit>().ToListAsync(ct);
        var categories = await db.Set<Category>().ToListAsync(ct);

        var products = new List<Product>();
        foreach (var chunk in skus.Chunk(ChunkSize))
            products.AddRange(await db.Set<Product>()
                .Where(p => chunk.Contains(p.SKU))
                .Include(p => p.Units).ThenInclude(u => u.Barcodes)
                .AsSplitQuery()
                .ToListAsync(ct));

        var existing = new List<ExistingBarcode>();
        foreach (var chunk in barcodes.Chunk(ChunkSize))
            existing.AddRange(await db.Set<ProductBarcode>().AsNoTracking()
                .Where(b => chunk.Contains(b.Barcode))
                .Select(b => new ExistingBarcode(b.Barcode, b.ProductUnit.Product.SKU, b.ProductUnit.Unit.Code, b.BarcodeType))
                .ToListAsync(ct));

        return new CatalogImportSnapshot(units, categories, products, existing);
    }

    public void Add(Unit unit) => db.Set<Unit>().Add(unit);

    public void Add(Category category) => db.Set<Category>().Add(category);

    public void Add(Product product) => db.Set<Product>().Add(product);

    public Task<Result> SaveAsync(CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
