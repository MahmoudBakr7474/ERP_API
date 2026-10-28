using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ListGoodsReceipts;

/// <param name="Search">Matches the receipt number, its stock document number or the supplier's delivery note.</param>
public sealed record ListGoodsReceiptsQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? SupplierId, Guid? PurchaseOrderId, string? Search) : IPagedQuery;
