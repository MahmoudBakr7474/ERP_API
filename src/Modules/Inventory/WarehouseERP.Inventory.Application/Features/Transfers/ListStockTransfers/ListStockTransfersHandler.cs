using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ListStockTransfers;

// Transfers whose source or destination is one of the user's warehouses; asking for another warehouse is 403.
public sealed class ListStockTransfersHandler(
    IStockTransferReadStore readStore, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockTransfersQuery, PagedResult<StockTransferSummaryResponse>>
{
    public async Task<Result<PagedResult<StockTransferSummaryResponse>>> HandleAsync(ListStockTransfersQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await readStore.ListAsync(
            new StockTransferFilter(WarehouseScope.Of(currentUser), query.WarehouseId, query.Status, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
