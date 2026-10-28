using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ListPurchaseRequests;

/// <param name="Orderable">Only approved requests with something left to order (to pick from when drafting an order).</param>
/// <param name="Search">Matches the number or the draft reference.</param>
public sealed record ListPurchaseRequestsQuery(
    int Page, int? PageSize, Guid? WarehouseId, PurchaseRequestStatus? Status, bool Orderable, string? Search) : IPagedQuery;
