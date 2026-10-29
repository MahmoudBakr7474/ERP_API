using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Deliveries.ListSalesDeliveries;

/// <param name="Search">Matches the number or the draft reference.</param>
public sealed record ListSalesDeliveriesQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? OrderId, Guid? CustomerId, SalesDeliveryStatus? Status, string? Search) : IPagedQuery;
