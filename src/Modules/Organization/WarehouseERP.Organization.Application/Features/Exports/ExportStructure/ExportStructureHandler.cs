using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportStructure;

// The structure report's own reads: the tree, and the location directory with the same status filter for the
// locations under it. Every printed line (branch, warehouse, location) counts against the file's row limit.
public sealed class ExportStructureHandler(IOrganizationReportReadStore reports, ICurrentUser user)
    : IQueryHandler<ExportStructureQuery, OrganizationStructureExport>
{
    public async Task<Result<OrganizationStructureExport>> HandleAsync(ExportStructureQuery query, CancellationToken ct)
    {
        var scope = WarehouseScope.Of(user);
        var branches = await reports.GetStructureAsync(query.IsActive, scope, ct);
        var lines = branches.Count + branches.Sum(b => b.Warehouses.Count);
        IReadOnlyList<LocationDirectoryResponse> locations = [];
        if (query.IncludeLocations && lines <= query.MaxRows)
        {
            // Only the warehouses the tree lists (already within scope and the status filter).
            var listed = branches.SelectMany(b => b.Warehouses).Select(w => w.Id).ToHashSet();
            var filter = new LocationDirectoryFilter(listed, null, null, null, null, query.IsActive);
            locations = listed.Count == 0 ? [] : (await reports.ListLocationsAsync(filter, 1, query.MaxRows - lines + 1, ct)).Items;
            lines += locations.Count;
        }
        return lines > query.MaxRows
            ? ExportErrors.TooManyRows(query.MaxRows)
            : new OrganizationStructureExport(branches, locations);
    }
}
