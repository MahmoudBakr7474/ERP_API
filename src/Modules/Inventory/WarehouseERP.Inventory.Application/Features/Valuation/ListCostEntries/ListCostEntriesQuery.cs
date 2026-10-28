using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Valuation.ListCostEntries;

public sealed record ListCostEntriesQuery(int Page, int? PageSize, Guid ProductId) : IPagedQuery;
