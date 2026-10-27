using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Products.ListProducts;

/// <param name="Search">Matches the SKU, either name, or a barcode exactly.</param>
public sealed record ListProductsQuery(
    int Page, int? PageSize, string? Search, Guid? CategoryId = null, ProductType? Type = null, bool? IsActive = null)
    : IPagedQuery;
