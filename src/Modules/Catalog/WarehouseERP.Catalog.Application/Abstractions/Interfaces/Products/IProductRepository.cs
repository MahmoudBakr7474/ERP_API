using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <summary>Write side of products; implemented over the shared ErpDbContext (no generic repository).</summary>
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
