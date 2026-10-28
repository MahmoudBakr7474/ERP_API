using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;
using WarehouseERP.Inventory.Application.Contracts.Valuation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;

// Valuation is company-wide (one average per product), so the permission alone decides who reads it.
public sealed class ListInventoryValuationHandler(IValuationReadStore valuation, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListInventoryValuationQuery, PagedResult<InventoryValuationResponse>>
{
    public async Task<Result<PagedResult<InventoryValuationResponse>>> HandleAsync(ListInventoryValuationQuery query, CancellationToken ct) =>
        await valuation.ListAsync(query.ProductId, query.IncludeZero, query.Page, query.ResolvePageSize(pagination.Value), ct);
}
