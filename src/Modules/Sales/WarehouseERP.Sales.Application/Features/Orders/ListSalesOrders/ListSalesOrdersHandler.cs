using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Contracts.Orders;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.ListSalesOrders;

// Orders of the warehouses in the user's scope, newest first.
public sealed class ListSalesOrdersHandler(ISalesOrderReadStore orders, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesOrdersQuery, PagedResult<SalesOrderSummaryResponse>>
{
    public async Task<Result<PagedResult<SalesOrderSummaryResponse>>> HandleAsync(ListSalesOrdersQuery query, CancellationToken ct) =>
        await orders.ListAsync(
            new SalesOrderFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.CustomerId, query.Status, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
