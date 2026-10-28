using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockBalances;

/// <param name="IncludeZero">Also list locations whose balance went back to zero.</param>
public sealed record ListStockBalancesQuery(
    int Page, int? PageSize, Guid? WarehouseId = null, Guid? LocationId = null, Guid? ProductId = null,
    bool IncludeZero = false) : IPagedQuery;
