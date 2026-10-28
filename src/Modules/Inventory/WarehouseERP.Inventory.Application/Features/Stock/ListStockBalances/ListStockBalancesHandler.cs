using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockBalances;

public sealed class ListStockBalancesHandler(
    IStockReadStore stock, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockBalancesQuery, PagedResult<StockBalanceResponse>>
{
    public async Task<Result<PagedResult<StockBalanceResponse>>> HandleAsync(ListStockBalancesQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await stock.ListBalancesAsync(
            new StockFilter(WarehouseScope.Of(currentUser), query.WarehouseId, query.LocationId, query.ProductId, query.IncludeZero),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
