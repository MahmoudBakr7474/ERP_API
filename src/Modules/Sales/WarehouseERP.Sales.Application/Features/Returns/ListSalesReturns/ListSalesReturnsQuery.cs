using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Features.Returns.ListSalesReturns;

/// <param name="Search">Matches the number or the draft reference.</param>
public sealed record ListSalesReturnsQuery(
    int Page, int? PageSize, Guid? WarehouseId, Guid? OrderId, Guid? DeliveryId, Guid? CustomerId, SalesReturnStatus? Status,
    string? Search) : IPagedQuery;
