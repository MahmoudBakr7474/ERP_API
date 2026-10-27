using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <summary>
/// Write side of products with their units and barcodes (one aggregate); implemented over the shared ErpDbContext
/// (no generic repository).
/// </summary>
public interface IProductRepository
{
    Task<bool> IsUnitActiveAsync(Guid unitId, CancellationToken ct);

    /// <summary>The unit's state, or null when no unit has this id.</summary>
    Task<UnitState?> GetUnitStateAsync(Guid unitId, CancellationToken ct);

    Task<bool> IsCategoryActiveAsync(Guid categoryId, CancellationToken ct);

    Task<bool> SkuExistsAsync(string sku, CancellationToken ct);

    /// <summary>True when any barcode, active or retired, has this value.</summary>
    Task<bool> BarcodeExistsAsync(string barcode, CancellationToken ct);

    /// <summary>Save point for a new product and its base unit row: one SaveChanges, one transaction.</summary>
    Task<Result> AddAsync(Product product, CancellationToken ct);

    /// <summary>
    /// The product, tracked for changes, or null. With a row version, the save fails with a conflict unless the
    /// product still has it.
    /// </summary>
    Task<Product?> GetForUpdateAsync(Guid productId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>The product with its units and their barcodes, tracked for changes, or null.</summary>
    Task<Product?> GetWithUnitsForUpdateAsync(Guid productId, CancellationToken ct);

    /// <summary>Save point for changes to products loaded with the methods above.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
