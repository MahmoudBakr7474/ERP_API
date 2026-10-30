using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;

// The organization's master data is small (branches and warehouses by the dozen), so every issue is found at once and
// the page cut here; the counts and the page come from the same list.
public sealed class ListOrganizationDataIssuesHandler(
    IOrganizationReportReadStore reports, ICurrentUser user, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListOrganizationDataIssuesQuery, OrganizationDataQualityResponse>
{
    public async Task<Result<OrganizationDataQualityResponse>> HandleAsync(ListOrganizationDataIssuesQuery query, CancellationToken ct)
    {
        var all = await reports.ListDataIssuesAsync(WarehouseScope.Of(user), ct);
        var counts = all.GroupBy(x => x.Issue).OrderBy(g => g.Key)
            .Select(g => new OrganizationDataIssueCount(g.Key, g.Count())).ToList();
        var matching = query.Issue is { } issue ? all.Where(x => x.Issue == issue).ToList() : all;
        var pageSize = query.ResolvePageSize(pagination.Value);
        var items = matching.Skip((query.Page - 1) * pageSize).Take(pageSize).ToList();
        return new OrganizationDataQualityResponse(
            new PagedResult<OrganizationDataIssueResponse>(items, query.Page, pageSize, matching.Count), counts);
    }
}
