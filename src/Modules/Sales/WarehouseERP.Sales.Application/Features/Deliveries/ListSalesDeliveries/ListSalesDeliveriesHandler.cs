using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.ListSalesDeliveries;

// Deliveries of the warehouses in the user's scope, newest first.
public sealed class ListSalesDeliveriesHandler(
    ISalesDeliveryReadStore deliveries, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesDeliveriesQuery, PagedResult<SalesDeliverySummaryResponse>>
{
    public async Task<Result<PagedResult<SalesDeliverySummaryResponse>>> HandleAsync(ListSalesDeliveriesQuery query, CancellationToken ct) =>
        await deliveries.ListAsync(
            new SalesDeliveryFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.OrderId, query.CustomerId, query.Status,
                Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
