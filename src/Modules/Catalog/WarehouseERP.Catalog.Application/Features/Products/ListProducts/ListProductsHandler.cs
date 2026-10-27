using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.ListProducts;

public sealed class ListProductsHandler(IProductReadStore products, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListProductsQuery, PagedResult<ProductSummaryResponse>>
{
    public async Task<Result<PagedResult<ProductSummaryResponse>>> HandleAsync(ListProductsQuery query, CancellationToken ct) =>
        await products.ListAsync(
            new ProductListFilter(Text.Optional(query.Search), query.CategoryId, query.Type, query.IsActive),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
