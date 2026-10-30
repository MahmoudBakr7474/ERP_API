using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportLocations;

// The same read as the location directory (scope, filters, order), over the whole result.
public sealed class ExportLocationsHandler(IOrganizationReportReadStore reports, ICurrentUser user)
    : IQueryHandler<ExportLocationsQuery, IReadOnlyList<LocationDirectoryResponse>>
{
    public async Task<Result<IReadOnlyList<LocationDirectoryResponse>>> HandleAsync(ExportLocationsQuery query, CancellationToken ct)
    {
        if (query.WarehouseId is { } warehouseId && !user.CanAccessWarehouse(warehouseId))
            return WarehouseErrors.AccessDenied;
        var filter = new LocationDirectoryFilter(
            WarehouseScope.Of(user), query.BranchId, query.WarehouseId, query.WarehouseType, Text.Optional(query.Search), query.IsActive,
            query.Sort ?? OrganizationSort.Code, query.Descending);
        var rows = await reports.ListLocationsAsync(filter, 1, query.MaxRows + 1, ct);
        return ExportLimit.Within(rows.Items, query.MaxRows);
    }
}
