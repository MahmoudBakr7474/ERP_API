namespace WarehouseERP.Catalog.Contracts;

/// <summary>Read-only view of products for other modules (Inventory, Purchasing, Sales, POS: scanning and searching at a till).</summary>
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

    /// <summary>Every unit of each of these products, active or not, with their conversion factors.</summary>
    Task<IReadOnlyList<ProductUnitInfo>> FindUnitsOfProductsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);

    /// <summary>
    /// The product unit an active barcode is printed on, when the barcode, the product unit, its unit and its product are
    /// all active (what a till may sell); null otherwise.
    /// </summary>
    Task<ProductUnitInfo?> FindUnitByBarcodeAsync(string barcode, CancellationToken ct);

    /// <summary>Up to <paramref name="take"/> active products whose SKU or name contains the text, or with that exact barcode, by SKU.</summary>
    Task<IReadOnlyList<ProductInfo>> SearchActiveAsync(string text, int take, CancellationToken ct);
}
