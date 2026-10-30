using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports.ExportDataQualityIssues;

public sealed class ExportDataQualityIssuesHandler(ICatalogReportReadStore reports) : IQueryHandler<ExportDataQualityIssuesQuery, IReadOnlyList<DataQualityIssueResponse>>
{
    public async Task<Result<IReadOnlyList<DataQualityIssueResponse>>> HandleAsync(ExportDataQualityIssuesQuery query, CancellationToken ct)
    {
        var all = await reports.ListDataQualityIssuesAsync(new DataQualityFilter(Text.Optional(query.Search), query.ProductIsActive), ct);
        var rows = query.Issue is { } issue ? all.Where(x => x.Issue == issue).ToList() : all;
        return ExportLimit.Within(rows, query.MaxRows);
    }
}
