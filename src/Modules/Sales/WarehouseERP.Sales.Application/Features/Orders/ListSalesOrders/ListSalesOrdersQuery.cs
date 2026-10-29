using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Orders.ListSalesOrders;

/// <param name="Search">Matches the number, the draft reference or the customer's reference.</param>
public sealed record ListSalesOrdersQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? CustomerId, SalesOrderStatus? Status, string? Search) : IPagedQuery;
