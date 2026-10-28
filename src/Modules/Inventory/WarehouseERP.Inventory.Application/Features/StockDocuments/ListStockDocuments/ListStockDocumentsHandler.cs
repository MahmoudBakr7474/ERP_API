using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ListStockDocuments;

// Only documents of the user's warehouses; asking for another warehouse is 403.
public sealed class ListStockDocumentsHandler(
    IStockDocumentReadStore readStore, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListStockDocumentsQuery, PagedResult<StockDocumentSummaryResponse>>
{
    public async Task<Result<PagedResult<StockDocumentSummaryResponse>>> HandleAsync(ListStockDocumentsQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !currentUser.CanAccessWarehouse(warehouseId))
            return StockErrors.WarehouseAccessDenied;
        return await readStore.ListAsync(
            new StockDocumentFilter(WarehouseScope.Of(currentUser), query.WarehouseId, query.DocumentType, query.Status, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
