using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Reports.GetOrganizationStructure;

// Company → branches → warehouses with location counts; the screen drills into locations through the location directory.
public sealed class GetOrganizationStructureHandler(IOrganizationReportReadStore reports, ICurrentUser user)
    : IQueryHandler<GetOrganizationStructureQuery, IReadOnlyList<StructureBranchResponse>>
{
    public async Task<Result<IReadOnlyList<StructureBranchResponse>>> HandleAsync(GetOrganizationStructureQuery query, CancellationToken ct) =>
        Result.Success(await reports.GetStructureAsync(query.IsActive, WarehouseScope.Of(user), ct));
}
