using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;

// Lists only the warehouses in the caller's scope.
public sealed class ListWarehousesHandler(
    IWarehouseReadStore warehouses, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListWarehousesQuery, PagedResult<WarehouseSummaryResponse>>
{
    public async Task<Result<PagedResult<WarehouseSummaryResponse>>> HandleAsync(ListWarehousesQuery query, CancellationToken ct)
    {
        var filter = new WarehouseListFilter(
            query.BranchId, currentUser.HasAllWarehouses ? null : currentUser.WarehouseIds, Text.Optional(query.Search), query.IsActive,
            query.Type, query.AllowsSelling, query.Sort ?? OrganizationSort.Code, query.Descending);
        return await warehouses.ListAsync(filter, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
