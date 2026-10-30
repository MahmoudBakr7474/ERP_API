using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportDataIssues;

// The same list as the data-quality screen, every row of the chosen issue (or all).
public sealed class ExportDataIssuesHandler(IOrganizationReportReadStore reports, ICurrentUser user)
    : IQueryHandler<ExportDataIssuesQuery, IReadOnlyList<OrganizationDataIssueResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationDataIssueResponse>>> HandleAsync(ExportDataIssuesQuery query, CancellationToken ct)
    {
        var all = await reports.ListDataIssuesAsync(WarehouseScope.Of(user), ct);
        var rows = query.Issue is { } issue ? all.Where(x => x.Issue == issue).ToList() : all;
        return ExportLimit.Within(rows, query.MaxRows);
    }
}
