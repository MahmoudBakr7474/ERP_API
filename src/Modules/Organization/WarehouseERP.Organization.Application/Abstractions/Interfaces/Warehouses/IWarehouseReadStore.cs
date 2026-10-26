using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Contracts.Warehouses;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <summary>Read side of warehouses: projections, no tracking.</summary>
public interface IWarehouseReadStore
{
    Task<WarehouseDetailsResponse?> GetAsync(Guid warehouseId, CancellationToken ct);

    Task<PagedResult<WarehouseSummaryResponse>> ListAsync(WarehouseListFilter filter, int page, int pageSize, CancellationToken ct);
}
