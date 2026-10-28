using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;

public sealed class ListCostEntriesHandler(IValuationReadStore valuation, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListCostEntriesQuery, PagedResult<InventoryCostEntryResponse>>
{
    public async Task<Result<PagedResult<InventoryCostEntryResponse>>> HandleAsync(ListCostEntriesQuery query, CancellationToken ct) =>
        await valuation.ListEntriesAsync(query.ProductId, query.Page, query.ResolvePageSize(pagination.Value), ct);
}
