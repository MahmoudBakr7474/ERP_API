namespace WarehouseERP.Catalog.Contracts;

// Read-only view of products for other modules (Inventory, Purchasing, Sales, POS).
public interface IProductCatalog
{
    Task<ProductInfo?> FindAsync(Guid productId, CancellationToken ct);
}

public sealed record ProductInfo(
    Guid Id, string Sku, string NameAr, Guid BaseUnitId, ProductType Type, TrackingMode TrackingMode, bool IsActive);
