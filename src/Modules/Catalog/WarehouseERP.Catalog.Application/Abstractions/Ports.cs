using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions;

// Ports this module's use cases need, implemented in WarehouseERP.Catalog over the shared
// ErpDbContext. Purpose-specific on purpose: no generic repository.

/// <summary>Write side of products.</summary>
public interface IProductRepository
{
    Task<bool> IsUnitActiveAsync(Guid unitId, CancellationToken ct);

    Task<bool> IsCategoryActiveAsync(Guid categoryId, CancellationToken ct);

    Task<bool> SkuExistsAsync(string sku, CancellationToken ct);

    /// <summary>Save point for a new product and its base unit row: one SaveChanges, one transaction.</summary>
    Task<Result> AddAsync(Product product, CancellationToken ct);

    /// <summary>Loads a product to change it; the save fails with a conflict unless its row version is still this one.</summary>
    Task<Product?> GetForUpdateAsync(Guid productId, byte[] expectedRowVersion, CancellationToken ct);

    /// <summary>Save point for changes to products loaded with <see cref="GetForUpdateAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}

/// <summary>Read side of products: projections, no tracking.</summary>
public interface IProductReadStore
{
    Task<ProductDetails?> GetAsync(Guid productId, CancellationToken ct);

    Task<PagedResult<ProductSummary>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
}

public sealed record ProductUnitItem(Guid Id, Guid UnitId, decimal ConversionFactor, bool IsActive);

/// <param name="RowVersion">Base64; send it back to update the product.</param>
public sealed record ProductDetails(
    Guid Id, string Sku, string NameAr, string? NameEn, Guid? CategoryId, Guid BaseUnitId, ProductType Type,
    TrackingMode TrackingMode, bool IsActive, string RowVersion, IReadOnlyList<ProductUnitItem> Units);

public sealed record ProductSummary(
    Guid Id, string Sku, string NameAr, string? NameEn, ProductType Type, TrackingMode TrackingMode, bool IsActive);
