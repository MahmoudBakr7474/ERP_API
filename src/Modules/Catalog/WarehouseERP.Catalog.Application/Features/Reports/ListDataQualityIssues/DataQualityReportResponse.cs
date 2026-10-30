using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Contracts.Reports;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListDataQualityIssues;

/// <summary>One page of issues, and how many of each kind exist with the same status and search filters.</summary>
public sealed record DataQualityReportResponse(PagedResult<DataQualityIssueResponse> Issues, IReadOnlyList<DataQualityIssueCount> Counts);
