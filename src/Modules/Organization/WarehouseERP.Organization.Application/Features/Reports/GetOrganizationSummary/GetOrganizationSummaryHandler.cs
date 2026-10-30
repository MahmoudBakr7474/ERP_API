using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Reports.GetOrganizationSummary;

// Counted by the database from the Organization tables at the time of the request, within the caller's scope.
public sealed class GetOrganizationSummaryHandler(IOrganizationReportReadStore reports, ICurrentUser user)
    : IQueryHandler<GetOrganizationSummaryQuery, OrganizationSummaryResponse>
{
    public async Task<Result<OrganizationSummaryResponse>> HandleAsync(GetOrganizationSummaryQuery query, CancellationToken ct) =>
        await reports.GetSummaryAsync(WarehouseScope.Of(user), ct);
}
