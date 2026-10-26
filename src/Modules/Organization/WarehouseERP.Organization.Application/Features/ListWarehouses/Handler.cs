using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.ListWarehouses;

// Lists only the warehouses in the caller's scope.
public sealed class ListWarehousesHandler(
    IWarehouseReadStore warehouses, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummary>>
{
    public async Task<Result<PagedResult<WarehouseSummary>>> HandleAsync(ListWarehousesQuery query, CancellationToken ct)
    {
        var filter = new WarehouseListFilter(query.BranchId, currentUser.HasAllWarehouses ? null : currentUser.WarehouseIds);
        return await warehouses.ListAsync(filter, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
