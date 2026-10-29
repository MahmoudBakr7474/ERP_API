using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Contracts.Prices;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Prices.ListSalesPrices;

// The price list is company-wide: the same prices for every warehouse and branch.
public sealed class ListSalesPricesHandler(ISalesPriceReadStore prices, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesPricesQuery, PagedResult<SalesPriceResponse>>
{
    public async Task<Result<PagedResult<SalesPriceResponse>>> HandleAsync(ListSalesPricesQuery query, CancellationToken ct) =>
        await prices.ListAsync(new SalesPriceFilter(query.ProductId, query.ProductUnitId), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
