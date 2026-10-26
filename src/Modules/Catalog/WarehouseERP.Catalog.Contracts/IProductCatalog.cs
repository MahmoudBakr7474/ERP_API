namespace WarehouseERP.Catalog.Contracts;

/// <summary>Read-only view of products for other modules (Inventory, Purchasing, Sales, POS).</summary>
public interface IProductCatalog
{
    /// <summary>The product, or null when no product has this id.</summary>
    Task<ProductInfo?> FindAsync(Guid productId, CancellationToken ct);
}
