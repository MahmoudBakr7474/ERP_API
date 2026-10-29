using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockReservations;

// Who holds a warehouse's reserved stock: the sales order lines and what each still holds. Read-only; reservations change
// only through Sales.
public sealed class ListStockReservationsHandler(
    IStockReadStore stock, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockReservationsQuery, PagedResult<StockReservationResponse>>
{
    public async Task<Result<PagedResult<StockReservationResponse>>> HandleAsync(ListStockReservationsQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await stock.ListReservationsAsync(
            new StockFilter(WarehouseScope.Of(currentUser), query.WarehouseId, LocationId: null, query.ProductId, query.IncludeClosed),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
