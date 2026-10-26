using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Catalog.Application.Features.Products.ListProducts;

/// <param name="Search">Matches SKU or either name.</param>
public sealed record ListProductsQuery(int Page, int? PageSize, string? Search) : IPagedQuery;
