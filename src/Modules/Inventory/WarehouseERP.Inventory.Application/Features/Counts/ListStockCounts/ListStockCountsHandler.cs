using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.ListStockCounts;

public sealed class ListStockCountsHandler(
    IStockCountReadStore readStore, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockCountsQuery, PagedResult<StockCountSummaryResponse>>
{
    public async Task<Result<PagedResult<StockCountSummaryResponse>>> HandleAsync(ListStockCountsQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await readStore.ListAsync(
            new StockCountFilter(WarehouseScope.Of(currentUser), query.WarehouseId, query.Status),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
