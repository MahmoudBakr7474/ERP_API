using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Contracts.Reports;

namespace WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;

/// <param name="Counts">Every kind of problem found, with its count, whatever the issue filter.</param>
public sealed record OrganizationDataQualityResponse(
    PagedResult<OrganizationDataIssueResponse> Issues, IReadOnlyList<OrganizationDataIssueCount> Counts);
