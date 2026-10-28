using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;

public sealed class ListStockMovementsHandler(
    IStockReadStore stock, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockMovementsQuery, PagedResult<StockMovementResponse>>
{
    public async Task<Result<PagedResult<StockMovementResponse>>> HandleAsync(ListStockMovementsQuery query, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(query.WarehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await stock.ListMovementsAsync(
            new StockMovementFilter(query.ProductId, query.WarehouseId, query.LocationId, query.FromUtc, query.ToUtc),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
