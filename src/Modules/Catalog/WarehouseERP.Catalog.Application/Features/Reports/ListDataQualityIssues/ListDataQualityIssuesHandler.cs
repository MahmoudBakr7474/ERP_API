using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListDataQualityIssues;

// The read store finds every issue of the selected products (a catalog has thousands of products, not millions);
// counting and paging them here keeps the counts and the page consistent with the export.
public sealed class ListDataQualityIssuesHandler(ICatalogReportReadStore reports, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListDataQualityIssuesQuery, DataQualityReportResponse>
{
    public async Task<Result<DataQualityReportResponse>> HandleAsync(ListDataQualityIssuesQuery query, CancellationToken ct)
    {
        var all = await reports.ListDataQualityIssuesAsync(new DataQualityFilter(Text.Optional(query.Search), query.ProductIsActive), ct);
        var counts = Enum.GetValues<CatalogDataIssue>().Select(i => new DataQualityIssueCount(i, all.Count(x => x.Issue == i))).ToList();
        var selected = query.Issue is { } issue ? all.Where(x => x.Issue == issue).ToList() : all;
        var pageSize = query.ResolvePageSize(pagination.Value);
        var items = selected.Skip((query.Page - 1) * pageSize).Take(pageSize).ToList();
        return new DataQualityReportResponse(new PagedResult<DataQualityIssueResponse>(items, query.Page, pageSize, selected.Count), counts);
    }
}
