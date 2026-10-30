using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListUnitConversions;

public sealed class ListUnitConversionsHandler(ICatalogReportReadStore reports, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListUnitConversionsQuery, PagedResult<UnitConversionResponse>>
{
    public async Task<Result<PagedResult<UnitConversionResponse>>> HandleAsync(ListUnitConversionsQuery query, CancellationToken ct) =>
        await reports.ListUnitConversionsAsync(
            new UnitConversionFilter(Text.Optional(query.Search), query.CategoryId, query.Type, query.ProductIsActive, query.UnitId),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
