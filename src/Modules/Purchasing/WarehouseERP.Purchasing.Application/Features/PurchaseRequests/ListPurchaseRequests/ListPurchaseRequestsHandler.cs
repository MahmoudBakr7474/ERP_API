using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseRequests.ListPurchaseRequests;

// Requests of the warehouses in the user's scope, newest first.
public sealed class ListPurchaseRequestsHandler(
    IPurchaseRequestReadStore requests, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListPurchaseRequestsQuery, PagedResult<PurchaseRequestSummaryResponse>>
{
    public async Task<Result<PagedResult<PurchaseRequestSummaryResponse>>> HandleAsync(ListPurchaseRequestsQuery query, CancellationToken ct) =>
        await requests.ListAsync(
            new PurchaseRequestFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.Status, query.Orderable, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
