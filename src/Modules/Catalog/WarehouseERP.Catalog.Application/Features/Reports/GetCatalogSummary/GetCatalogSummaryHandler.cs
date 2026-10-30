using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Reports.GetCatalogSummary;

public sealed class GetCatalogSummaryHandler(ICatalogReportReadStore reports)
    : IQueryHandler<GetCatalogSummaryQuery, CatalogSummaryResponse>
{
    public async Task<Result<CatalogSummaryResponse>> HandleAsync(GetCatalogSummaryQuery query, CancellationToken ct) =>
        await reports.GetSummaryAsync(ct);
}
