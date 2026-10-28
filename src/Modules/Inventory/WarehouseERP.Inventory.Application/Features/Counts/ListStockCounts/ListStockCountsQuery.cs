using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.Counts.ListStockCounts;

public sealed record ListStockCountsQuery(int Page, int? PageSize, Guid? WarehouseId = null, StockCountStatus? Status = null) : IPagedQuery;
