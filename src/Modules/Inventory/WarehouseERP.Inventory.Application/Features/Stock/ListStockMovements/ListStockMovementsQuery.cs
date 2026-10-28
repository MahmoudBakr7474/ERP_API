using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Inventory.Application.Features.Stock.ListStockMovements;

/// <summary>The stock card of one product in one warehouse (optionally one location), oldest first.</summary>
/// <param name="FromUtc">Inclusive.</param>
/// <param name="ToUtc">Exclusive.</param>
public sealed record ListStockMovementsQuery(
    int Page, int? PageSize, Guid ProductId, Guid WarehouseId, Guid? LocationId = null, DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IPagedQuery;
