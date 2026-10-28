using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListInventoryValuation;

/// <param name="IncludeZero">Also list products the company no longer holds.</param>
public sealed record ListInventoryValuationQuery(int Page, int? PageSize, Guid? ProductId = null, bool IncludeZero = false) : IPagedQuery;
