using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListCategoryProductCounts;

// Categories are few (ADR 0014), so the report is one unpaged list in tree order.
public sealed class ListCategoryProductCountsHandler(ICatalogReportReadStore reports)
    : IQueryHandler<ListCategoryProductCountsQuery, IReadOnlyList<CategoryProductCountResponse>>
{
    public async Task<Result<IReadOnlyList<CategoryProductCountResponse>>> HandleAsync(
        ListCategoryProductCountsQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<CategoryProductCountResponse>>(await reports.ListCategoryProductCountsAsync(query.IsActive, ct));
}
