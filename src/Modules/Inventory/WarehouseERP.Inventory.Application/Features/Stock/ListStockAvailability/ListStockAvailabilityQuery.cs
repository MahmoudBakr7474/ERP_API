using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockAvailability;

/// <param name="IncludeZero">Also list products with nothing on hand and nothing reserved.</param>
public sealed record ListStockAvailabilityQuery(
    int Page, int? PageSize, Guid? WarehouseId = null, Guid? ProductId = null, bool IncludeZero = false) : IPagedQuery;
