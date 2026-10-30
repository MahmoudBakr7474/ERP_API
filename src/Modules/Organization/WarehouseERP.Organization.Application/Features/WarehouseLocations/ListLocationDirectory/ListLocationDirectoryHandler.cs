using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.WarehouseLocations.ListLocationDirectory;

// Only locations of warehouses in the caller's scope; asking for one warehouse outside it is refused like reading it.
public sealed class ListLocationDirectoryHandler(
    IOrganizationReportReadStore reports, ICurrentUser user, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListLocationDirectoryQuery, PagedResult<LocationDirectoryResponse>>
{
    public async Task<Result<PagedResult<LocationDirectoryResponse>>> HandleAsync(ListLocationDirectoryQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !user.CanAccessWarehouse(warehouseId))
            return WarehouseErrors.AccessDenied;
        var filter = new LocationDirectoryFilter(
            WarehouseScope.Of(user), query.BranchId, query.WarehouseId, query.WarehouseType, Text.Optional(query.Search), query.IsActive,
            query.Sort ?? OrganizationSort.Code, query.Descending);
        return await reports.ListLocationsAsync(filter, query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
