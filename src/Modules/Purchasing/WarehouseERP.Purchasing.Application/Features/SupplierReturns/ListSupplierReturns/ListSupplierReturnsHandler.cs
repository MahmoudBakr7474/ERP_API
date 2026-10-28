using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ListSupplierReturns;

// Returns of the warehouses in the user's scope, newest first; with a credit claim filter for future Accounting.
public sealed class ListSupplierReturnsHandler(
    ISupplierReturnReadStore returns, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSupplierReturnsQuery, PagedResult<SupplierReturnSummaryResponse>>
{
    public async Task<Result<PagedResult<SupplierReturnSummaryResponse>>> HandleAsync(ListSupplierReturnsQuery query, CancellationToken ct) =>
        await returns.ListAsync(
            new SupplierReturnFilter(DocumentAccess.ScopeOf(currentUser), query.SupplierId, query.PurchaseOrderId, query.GoodsReceiptId,
                query.Status, query.WithCreditClaim, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
