using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Infrastructure.Persistence;

// Catalog reports, read without tracking. Each data-quality rule is written once (the predicates below) and used by
// both the summary counts and the issue list, so the dashboard and the report never disagree.
internal sealed class CatalogReportReadStore(DbContext db) : ICatalogReportReadStore
{
    private static readonly Expression<Func<Product, ProductMasterResponse>> ProductMaster = x => new ProductMasterResponse(
        x.Id, x.SKU, x.NameAr, x.NameEn, x.ProductType, x.TrackingMode, x.IsActive,
        x.Category != null ? x.Category.Code : null,
        x.Category != null ? x.Category.NameAr : null,
        x.Category != null ? x.Category.NameEn : null,
        x.BaseUnit.Code, x.BaseUnit.NameAr, x.BaseUnit.NameEn,
        x.Units.Count(u => u.IsActive),
        x.Units.Where(u => u.IsActive).SelectMany(u => u.Barcodes).Count(b => b.IsActive),
        x.CreatedAtUtc);

    public async Task<CatalogSummaryResponse> GetSummaryAsync(CancellationToken ct)
    {
        var products = db.Set<Product>().AsNoTracking();
        var byType = await products.GroupBy(x => new { x.ProductType, x.IsActive })
            .Select(g => new { g.Key.ProductType, g.Key.IsActive, Count = g.Count() })
            .ToListAsync(ct);
        var categories = await db.Set<Category>().AsNoTracking().GroupBy(x => x.IsActive)
            .Select(g => new { IsActive = g.Key, Count = g.Count() }).ToListAsync(ct);
        var units = await db.Set<Unit>().AsNoTracking().GroupBy(x => x.IsActive)
            .Select(g => new { IsActive = g.Key, Count = g.Count() }).ToListAsync(ct);
        var active = products.Where(x => x.IsActive);
        var barcodes = await db.Set<ProductBarcode>().AsNoTracking()
            .CountAsync(b => b.IsActive && b.ProductUnit.IsActive && b.ProductUnit.Product.IsActive, ct);

        return new CatalogSummaryResponse(
            Products: byType.Sum(x => x.Count),
            ActiveProducts: byType.Where(x => x.IsActive).Sum(x => x.Count),
            InactiveProducts: byType.Where(x => !x.IsActive).Sum(x => x.Count),
            StockItems: byType.Where(x => x.ProductType == ProductType.StockItem).Sum(x => x.Count),
            NonStockItems: byType.Where(x => x.ProductType == ProductType.NonStockItem).Sum(x => x.Count),
            Services: byType.Where(x => x.ProductType == ProductType.Service).Sum(x => x.Count),
            Categories: categories.Sum(x => x.Count),
            ActiveCategories: categories.Where(x => x.IsActive).Sum(x => x.Count),
            Units: units.Sum(x => x.Count),
            ActiveUnits: units.Where(x => x.IsActive).Sum(x => x.Count),
            ActiveBarcodes: barcodes,
            ProductsWithoutBarcode: await WithoutBarcode(active).CountAsync(ct),
            ProductsWithoutCategory: await active.CountAsync(x => x.CategoryId == null, ct),
            ProductsWithUnitIssues: await active.CountAsync(
                x => !x.BaseUnit.IsActive
                     || x.Units.Any(u => u.IsActive && !u.Unit.IsActive && u.UnitId != x.BaseUnitId)
                     || (!x.BaseUnit.AllowsFraction && x.Units.Any(u => u.ConversionFactor != Math.Floor(u.ConversionFactor))),
                ct));
    }

    public async Task<PagedResult<ProductMasterResponse>> ListProductsAsync(
        ProductListFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var query = ProductReadStore.Filtered(db.Set<Product>().AsNoTracking(), filter);
        var total = await query.CountAsync(ct);
        var items = await ProductReadStore.Sorted(query, filter)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ProductMaster)
            .ToListAsync(ct);
        return new PagedResult<ProductMasterResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<UnitConversionResponse>> ListUnitConversionsAsync(
        UnitConversionFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var productIds = ProductsMatching(filter.Search, filter.CategoryId, filter.Type, filter.ProductIsActive).Select(p => p.Id);
        var query = db.Set<ProductUnit>().AsNoTracking().Where(u => productIds.Contains(u.ProductId));
        if (filter.UnitId is { } unitId)
            query = query.Where(u => u.UnitId == unitId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Product.SKU).ThenBy(u => u.ConversionFactor).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UnitConversionResponse(
                u.ProductId, u.Product.SKU, u.Product.NameAr, u.Product.NameEn, u.Product.IsActive,
                u.Id, u.Unit.Code, u.Unit.NameAr, u.Unit.NameEn, u.ConversionFactor, u.UnitId == u.Product.BaseUnitId,
                u.IsActive, u.Product.BaseUnit.Code, u.Product.BaseUnit.NameAr, u.Product.BaseUnit.NameEn,
                u.Barcodes.Count(b => b.IsActive)))
            .ToListAsync(ct);
        return new PagedResult<UnitConversionResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<BarcodeReportResponse>> ListBarcodesAsync(
        BarcodeReportFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var productIds = ProductsMatching(null, filter.CategoryId, filter.Type, filter.ProductIsActive).Select(p => p.Id);
        var query = db.Set<ProductBarcode>().AsNoTracking().Where(b => productIds.Contains(b.ProductUnit.ProductId));
        if (filter.Search is { } search)
            query = query.Where(b => b.Barcode.Contains(search) || b.ProductUnit.Product.SKU.Contains(search)
                                     || b.ProductUnit.Product.NameAr.Contains(search)
                                     || (b.ProductUnit.Product.NameEn != null && b.ProductUnit.Product.NameEn.Contains(search)));
        if (filter.BarcodeType is { } type)
            query = query.Where(b => b.BarcodeType == type);
        if (filter.IsActive is { } isActive)
            query = query.Where(b => b.IsActive == isActive);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(b => b.ProductUnit.Product.SKU).ThenBy(b => b.ProductUnit.ConversionFactor).ThenBy(b => b.Barcode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BarcodeReportResponse(
                b.ProductUnit.ProductId, b.ProductUnit.Product.SKU, b.ProductUnit.Product.NameAr, b.ProductUnit.Product.NameEn,
                b.ProductUnit.Unit.Code, b.ProductUnit.Unit.NameAr, b.ProductUnit.Unit.NameEn, b.ProductUnit.ConversionFactor,
                b.Barcode, b.BarcodeType, b.IsActive))
            .ToListAsync(ct);
        return new PagedResult<BarcodeReportResponse>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<CategoryProductCountResponse>> ListCategoryProductCountsAsync(
        bool? categoryIsActive, CancellationToken ct)
    {
        var categories = await db.Set<Category>().AsNoTracking()
            .Select(c => new { c.Id, c.ParentId, c.Code, c.NameAr, c.NameEn, c.IsActive })
            .ToListAsync(ct);
        var counts = (await db.Set<Product>().AsNoTracking()
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Active = g.Count(p => p.IsActive), Inactive = g.Count(p => !p.IsActive) })
                .ToListAsync(ct))
            .ToDictionary(x => x.CategoryId ?? Guid.Empty);

        var byId = categories.ToDictionary(c => c.Id);
        var children = categories.ToLookup(c => c.ParentId);
        var rows = new List<CategoryProductCountResponse>();
        var visited = new HashSet<Guid>();

        // Tree order: each parent, then its children by code. The visited set guards against a loop in bad data.
        void Walk(Guid? parentId, int level)
        {
            foreach (var c in children[parentId].OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase))
            {
                if (!visited.Add(c.Id))
                    continue;
                counts.TryGetValue(c.Id, out var count);
                var parentCode = c.ParentId is { } p && byId.TryGetValue(p, out var parent) ? parent.Code : null;
                rows.Add(new CategoryProductCountResponse(c.Id, c.Code, c.NameAr, c.NameEn, c.ParentId, parentCode, level, c.IsActive,
                    count?.Active ?? 0, count?.Inactive ?? 0));
                Walk(c.Id, level + 1);
            }
        }

        Walk(null, 0);
        var selected = rows.Where(r => categoryIsActive is not { } active || r.IsActive == active).ToList();
        if (categoryIsActive is not false && counts.TryGetValue(Guid.Empty, out var none))
            selected.Add(new CategoryProductCountResponse(null, null, null, null, null, null, 0, true, none.Active, none.Inactive));
        return selected;
    }

    public async Task<IReadOnlyList<UnitUsageResponse>> ListUnitUsageAsync(bool? isActive, CancellationToken ct)
    {
        var query = db.Set<Unit>().AsNoTracking();
        if (isActive is { } active)
            query = query.Where(u => u.IsActive == active);
        return await query.OrderBy(u => u.Code)
            .Select(u => new UnitUsageResponse(u.Id, u.Code, u.NameAr, u.NameEn, u.AllowsFraction, u.IsActive,
                db.Set<Product>().Count(p => p.BaseUnitId == u.Id),
                db.Set<ProductUnit>().Count(pu => pu.UnitId == u.Id)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DataQualityIssueResponse>> ListDataQualityIssuesAsync(DataQualityFilter filter, CancellationToken ct)
    {
        var products = ProductsMatching(filter.Search, null, null, filter.ProductIsActive);
        var issues = new List<DataQualityIssueResponse>();

        void Add(CatalogDataIssue issue, IEnumerable<(Guid Id, string Sku, string NameAr, string? NameEn, bool IsActive, string? Detail)> rows) =>
            issues.AddRange(rows.Select(x => new DataQualityIssueResponse(x.Id, x.Sku, x.NameAr, x.NameEn, x.IsActive, issue, x.Detail)));

        Add(CatalogDataIssue.NoBarcode, (await WithoutBarcode(products)
                .Select(p => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive }).ToListAsync(ct))
            .Select(p => (p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, (string?)null)));
        Add(CatalogDataIssue.NoCategory, (await products.Where(p => p.CategoryId == null)
                .Select(p => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive }).ToListAsync(ct))
            .Select(p => (p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, (string?)null)));
        Add(CatalogDataIssue.InactiveCategory, (await products.Where(p => p.Category != null && !p.Category.IsActive)
                .Select(p => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, p.Category!.Code }).ToListAsync(ct))
            .Select(p => (p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, (string?)p.Code)));
        Add(CatalogDataIssue.InactiveBaseUnit, (await products.Where(p => !p.BaseUnit.IsActive)
                .Select(p => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, p.BaseUnit.Code }).ToListAsync(ct))
            .Select(p => (p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, (string?)p.Code)));
        Add(CatalogDataIssue.MissingEnglishName, (await products.Where(p => p.NameEn == null)
                .Select(p => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive }).ToListAsync(ct))
            .Select(p => (p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, (string?)null)));

        // Unit rules are per product unit: one row for each unit concerned.
        var retired = await products
            .SelectMany(p => p.Units.Where(u => u.IsActive && !u.Unit.IsActive && u.UnitId != p.BaseUnitId),
                (p, u) => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, u.Unit.Code })
            .ToListAsync(ct);
        issues.AddRange(retired.Select(x => new DataQualityIssueResponse(
            x.Id, x.SKU, x.NameAr, x.NameEn, x.IsActive, CatalogDataIssue.InactiveUnitInUse, x.Code)));
        var fractional = await products.Where(p => !p.BaseUnit.AllowsFraction)
            .SelectMany(p => p.Units.Where(u => u.ConversionFactor != Math.Floor(u.ConversionFactor)),
                (p, u) => new { p.Id, p.SKU, p.NameAr, p.NameEn, p.IsActive, u.Unit.Code, u.ConversionFactor, BaseCode = p.BaseUnit.Code })
            .ToListAsync(ct);
        issues.AddRange(fractional.Select(x => new DataQualityIssueResponse(
            x.Id, x.SKU, x.NameAr, x.NameEn, x.IsActive, CatalogDataIssue.FractionalFactorOnWholeUnit,
            string.Create(CultureInfo.InvariantCulture, $"1 {x.Code} = {x.ConversionFactor:0.########} {x.BaseCode}"))));

        return issues.OrderBy(x => x.Sku, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Issue).ThenBy(x => x.Detail).ToList();
    }

    public async Task<ProductSheetResponse?> GetProductSheetAsync(Guid productId, CancellationToken ct)
    {
        var product = await db.Set<Product>().AsNoTracking().Where(x => x.Id == productId).Select(ProductMaster).SingleOrDefaultAsync(ct);
        if (product is null)
            return null;
        var updatedAt = await db.Set<Product>().AsNoTracking().Where(x => x.Id == productId).Select(x => x.UpdatedAtUtc).SingleAsync(ct);
        var units = await db.Set<ProductUnit>().AsNoTracking()
            .Where(u => u.ProductId == productId)
            .OrderBy(u => u.ConversionFactor).ThenBy(u => u.Id)
            .Select(u => new ProductUnitResponse(
                u.Id, u.UnitId, u.ConversionFactor, u.IsActive, u.Unit.Code, u.Unit.NameAr, u.Unit.NameEn,
                u.UnitId == u.Product.BaseUnitId,
                u.Barcodes.OrderBy(b => b.Barcode)
                    .Select(b => new ProductBarcodeResponse(b.Id, b.Barcode, b.BarcodeType, b.IsActive))
                    .ToList()))
            .AsSplitQuery()
            .ToListAsync(ct);
        return new ProductSheetResponse(product, units, updatedAt);
    }

    // A stock or non-stock item with no active barcode on an active unit; services are not expected to carry one.
    private static IQueryable<Product> WithoutBarcode(IQueryable<Product> products) =>
        products.Where(p => p.ProductType != ProductType.Service
                            && !p.Units.Any(u => u.IsActive && u.Barcodes.Any(b => b.IsActive)));

    private IQueryable<Product> ProductsMatching(string? search, Guid? categoryId, ProductType? type, bool? isActive)
    {
        var query = db.Set<Product>().AsNoTracking();
        if (search is { } s)
            query = query.Where(x => x.SKU.Contains(s) || x.NameAr.Contains(s) || (x.NameEn != null && x.NameEn.Contains(s)));
        if (categoryId is { } c)
            query = query.Where(x => x.CategoryId == c);
        if (type is { } t)
            query = query.Where(x => x.ProductType == t);
        if (isActive is { } a)
            query = query.Where(x => x.IsActive == a);
        return query;
    }
}
