using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockReservations;

/// <param name="IncludeClosed">Also list reservations that were fully delivered or released.</param>
public sealed record ListStockReservationsQuery(
    int Page, int? PageSize, Guid? WarehouseId = null, Guid? ProductId = null, bool IncludeClosed = false) : IPagedQuery;
