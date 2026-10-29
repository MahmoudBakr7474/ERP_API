using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.ListSalesReturns;

// Customer returns of the warehouses in the user's scope, newest first.
public sealed class ListSalesReturnsHandler(ISalesReturnReadStore returns, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSalesReturnsQuery, PagedResult<SalesReturnSummaryResponse>>
{
    public async Task<Result<PagedResult<SalesReturnSummaryResponse>>> HandleAsync(ListSalesReturnsQuery query, CancellationToken ct) =>
        await returns.ListAsync(
            new SalesReturnFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.OrderId, query.DeliveryId, query.CustomerId,
                query.Status, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
