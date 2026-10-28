namespace WarehouseERP.Catalog.Contracts;

/// <summary>Read-only view of products for other modules (Inventory, Purchasing, Sales, POS).</summary>
public interface IProductCatalog
{
    /// <summary>The product, or null when no product has this id.</summary>
    Task<ProductInfo?> FindAsync(Guid productId, CancellationToken ct);

    /// <summary>The products among these ids that exist; unknown ids are left out.</summary>
    Task<IReadOnlyList<ProductInfo>> FindManyAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);

    /// <summary>The product units among these ids that exist, with their conversion factors; unknown ids are left out.</summary>
    Task<IReadOnlyList<ProductUnitInfo>> FindUnitsAsync(IReadOnlyCollection<Guid> productUnitIds, CancellationToken ct);

    /// <summary>The base unit of each of these products that exists.</summary>
    Task<IReadOnlyList<ProductUnitInfo>> FindBaseUnitsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);
}
