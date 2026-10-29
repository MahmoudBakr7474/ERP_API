using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Contracts.Stock;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Stock;

public interface IStockReadStore
{
    Task<PagedResult<StockBalanceResponse>> ListBalancesAsync(StockFilter filter, int page, int pageSize, CancellationToken ct);

    Task<PagedResult<StockAvailabilityResponse>> ListAvailabilityAsync(StockFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>The stock card: oldest first, each with the balance it left.</summary>
    Task<PagedResult<StockMovementResponse>> ListMovementsAsync(StockMovementFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<PagedResult<OverReservationResponse>> ListOverReservationsAsync(StockFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Oldest first; without IncludeZero only reservations that still hold something.</summary>
    Task<PagedResult<StockReservationResponse>> ListReservationsAsync(StockFilter filter, int page, int pageSize, CancellationToken ct);
}
