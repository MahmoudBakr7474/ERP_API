using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Contracts.Products;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;

/// <summary>Read side of products: projections, no tracking.</summary>
public interface IProductReadStore
{
    Task<ProductDetailsResponse?> GetAsync(Guid productId, CancellationToken ct);

    Task<PagedResult<ProductSummaryResponse>> ListAsync(string? search, int page, int pageSize, CancellationToken ct);
}
