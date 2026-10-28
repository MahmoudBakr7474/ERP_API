using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListOverReservations;

public sealed record ListOverReservationsQuery(int Page, int? PageSize, Guid? WarehouseId = null, Guid? ProductId = null) : IPagedQuery;
