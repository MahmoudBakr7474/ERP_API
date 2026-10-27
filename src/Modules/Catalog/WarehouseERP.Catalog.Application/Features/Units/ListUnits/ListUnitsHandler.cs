using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;
using WarehouseERP.Catalog.Application.Contracts.Units;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Units.ListUnits;

public sealed class ListUnitsHandler(IUnitReadStore units, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListUnitsQuery, PagedResult<UnitSummaryResponse>>
{
    public async Task<Result<PagedResult<UnitSummaryResponse>>> HandleAsync(ListUnitsQuery query, CancellationToken ct) =>
        await units.ListAsync(
            new UnitListFilter(Text.Optional(query.Search), query.IsActive), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
