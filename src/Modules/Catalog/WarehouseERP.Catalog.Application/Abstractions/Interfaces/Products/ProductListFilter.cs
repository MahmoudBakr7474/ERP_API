using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <param name="Search">Matches the SKU, either name, or a barcode exactly.</param>
/// <param name="Sort">The order; ties fall back to the SKU, which is unique, so pages never overlap.</param>
public sealed record ProductListFilter(
    string? Search, Guid? CategoryId, ProductType? Type, bool? IsActive, ProductSort Sort = ProductSort.Sku, bool Descending = false);
