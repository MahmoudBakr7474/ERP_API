using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Application.Contracts.Units;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;

/// <summary>Read side of units: projections, no tracking.</summary>
public interface IUnitReadStore
{
    Task<UnitDetailsResponse?> GetAsync(Guid unitId, CancellationToken ct);

    Task<PagedResult<UnitSummaryResponse>> ListAsync(UnitListFilter filter, int page, int pageSize, CancellationToken ct);
}
