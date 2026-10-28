using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListOverReservations;

public sealed class ListOverReservationsHandler(
    IStockReadStore stock, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListOverReservationsQuery, PagedResult<OverReservationResponse>>
{
    public async Task<Result<PagedResult<OverReservationResponse>>> HandleAsync(ListOverReservationsQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await stock.ListOverReservationsAsync(
            new StockFilter(WarehouseScope.Of(currentUser), query.WarehouseId, LocationId: null, query.ProductId, IncludeZero: true),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
