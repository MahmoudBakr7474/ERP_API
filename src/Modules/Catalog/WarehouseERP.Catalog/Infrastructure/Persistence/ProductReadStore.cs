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
                Units = x.Units.OrderBy(u => u.ConversionFactor).ThenBy(u => u.Id)
                    .Select(u => new ProductUnitResponse(
                        u.Id, u.UnitId, u.ConversionFactor, u.IsActive, u.Unit.Code, u.Unit.NameAr, u.Unit.NameEn,
                        u.UnitId == x.BaseUnitId,
                        u.Barcodes.OrderBy(b => b.Barcode)
                            .Select(b => new ProductBarcodeResponse(b.Id, b.Barcode, b.BarcodeType, b.IsActive))
                            .ToList()))
                    .ToList(),
            })
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);
        return row is null
            ? null
            : new ProductDetailsResponse(row.Id, row.SKU, row.NameAr, row.NameEn, row.CategoryId, row.BaseUnitId, row.ProductType,
                row.TrackingMode, row.IsActive, Convert.ToBase64String(row.RowVersion), row.Units);
    }

    public async Task<PagedResult<ProductSummaryResponse>> ListAsync(
        ProductListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Product>().AsNoTracking();
        if (filter.Search is { } search)
            query = query.Where(x => x.SKU.Contains(search) || x.NameAr.Contains(search)
                                     || (x.NameEn != null && x.NameEn.Contains(search))
                                     || x.Units.Any(u => u.Barcodes.Any(b => b.Barcode == search)));
        if (filter.CategoryId is { } categoryId)
            query = query.Where(x => x.CategoryId == categoryId);
        if (filter.Type is { } type)
            query = query.Where(x => x.ProductType == type);
        if (filter.IsActive is { } isActive)
            query = query.Where(x => x.IsActive == isActive);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.SKU)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductSummaryResponse(
                x.Id, x.SKU, x.NameAr, x.NameEn, x.ProductType, x.TrackingMode, x.IsActive, x.CategoryId, x.BaseUnitId))
            .ToListAsync(ct);
        return new PagedResult<ProductSummaryResponse>(items, page, pageSize, total);
    }

    public Task<ProductLookupResponse?> FindByBarcodeAsync(string barcode, CancellationToken ct) =>
        db.Set<ProductBarcode>().AsNoTracking()
            .Where(b => b.Barcode == barcode && b.IsActive
                        && b.ProductUnit.IsActive && b.ProductUnit.Product.IsActive && b.ProductUnit.Unit.IsActive)
            .Select(b => new ProductLookupResponse(
                b.ProductUnit.ProductId, b.ProductUnit.Product.SKU, b.ProductUnit.Product.NameAr, b.ProductUnit.Product.NameEn,
                b.ProductUnit.Product.ProductType, b.ProductUnit.Product.TrackingMode,
                b.ProductUnitId, b.ProductUnit.UnitId, b.ProductUnit.Unit.Code, b.ProductUnit.Unit.NameAr, b.ProductUnit.Unit.NameEn,
                b.ProductUnit.ConversionFactor, b.ProductUnit.UnitId == b.ProductUnit.Product.BaseUnitId,
                b.Id, b.Barcode, b.BarcodeType))
            .SingleOrDefaultAsync(ct);
}
