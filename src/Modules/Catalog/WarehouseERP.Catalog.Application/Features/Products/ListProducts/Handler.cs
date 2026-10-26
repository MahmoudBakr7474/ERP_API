using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.ListProducts;

public sealed class ListProductsHandler(IProductReadStore products, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListProductsQuery, PagedResult<ProductSummary>>
{
    public async Task<Result<PagedResult<ProductSummary>>> HandleAsync(ListProductsQuery query, CancellationToken ct) =>
        await products.ListAsync(Text.Optional(query.Search), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
