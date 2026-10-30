using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;

/// <param name="Issue">Only this kind of problem; the counts always cover every kind.</param>
public sealed record ListOrganizationDataIssuesQuery(OrganizationDataIssue? Issue, int Page, int? PageSize) : IPagedQuery;
