using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ListPurchaseOrders;

// Orders of the warehouses in the user's scope, newest first.
public sealed class ListPurchaseOrdersHandler(IPurchaseOrderReadStore orders, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPurchaseOrdersQuery, PagedResult<PurchaseOrderSummaryResponse>>
{
    public async Task<Result<PagedResult<PurchaseOrderSummaryResponse>>> HandleAsync(ListPurchaseOrdersQuery query, CancellationToken ct) =>
        await orders.ListAsync(
            new PurchaseOrderFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.SupplierId, query.Status, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
