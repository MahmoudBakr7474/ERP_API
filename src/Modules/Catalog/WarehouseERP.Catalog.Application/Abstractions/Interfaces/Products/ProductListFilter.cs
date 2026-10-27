using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <param name="Search">Matches the SKU, either name, or a barcode exactly.</param>
public sealed record ProductListFilter(string? Search, Guid? CategoryId, ProductType? Type, bool? IsActive);
