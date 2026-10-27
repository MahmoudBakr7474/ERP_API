using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListWarehouseLocations;

// The default location first, then by code. Warehouse scope before existence, as for the warehouse itself.
public sealed class ListWarehouseLocationsHandler(
    IWarehouseReadStore warehouses, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListWarehouseLocationsQuery, PagedResult<WarehouseLocationResponse>>
{
    public async Task<Result<PagedResult<WarehouseLocationResponse>>> HandleAsync(ListWarehouseLocationsQuery query, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(query.WarehouseId))
            return WarehouseErrors.AccessDenied;
        if (!await warehouses.ExistsAsync(query.WarehouseId, ct))
            return WarehouseErrors.NotFound;

        return await warehouses.ListLocationsAsync(
            query.WarehouseId, Text.Optional(query.Search), query.IsActive, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
