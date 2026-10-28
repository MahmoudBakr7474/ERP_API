using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ListPurchaseOrders;

/// <param name="Search">Matches the number, the draft reference or the supplier's reference.</param>
public sealed record ListPurchaseOrdersQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? SupplierId, PurchaseOrderStatus? Status, string? Search) : IPagedQuery;
