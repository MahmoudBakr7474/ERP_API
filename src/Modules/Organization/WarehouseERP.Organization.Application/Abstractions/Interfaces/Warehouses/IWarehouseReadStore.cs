using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Application.Contracts.Warehouses;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <summary>Read side of warehouses and their locations: projections, no tracking.</summary>
public interface IWarehouseReadStore
{
    Task<WarehouseDetailsResponse?> GetAsync(Guid warehouseId, CancellationToken ct);

    Task<PagedResult<WarehouseSummaryResponse>> ListAsync(WarehouseListFilter filter, int page, int pageSize, CancellationToken ct);

    Task<bool> ExistsAsync(Guid warehouseId, CancellationToken ct);

    /// <summary>The location, or null when the warehouse has no location with this id.</summary>
    Task<WarehouseLocationResponse?> GetLocationAsync(Guid warehouseId, Guid locationId, CancellationToken ct);

    /// <param name="search">Matches the code or either name.</param>
    Task<PagedResult<WarehouseLocationResponse>> ListLocationsAsync(
        Guid warehouseId, string? search, bool? isActive, int page, int pageSize, CancellationToken ct);
}
