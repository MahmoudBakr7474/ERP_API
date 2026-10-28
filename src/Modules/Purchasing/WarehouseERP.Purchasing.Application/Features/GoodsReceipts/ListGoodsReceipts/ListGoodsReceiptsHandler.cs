using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ListGoodsReceipts;

// Receipts of the warehouses in the user's scope, newest first.
public sealed class ListGoodsReceiptsHandler(IGoodsReceiptReadStore receipts, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListGoodsReceiptsQuery, PagedResult<GoodsReceiptSummaryResponse>>
{
    public async Task<Result<PagedResult<GoodsReceiptSummaryResponse>>> HandleAsync(ListGoodsReceiptsQuery query, CancellationToken ct) =>
        await receipts.ListAsync(
            new GoodsReceiptFilter(DocumentAccess.ScopeOf(currentUser), query.WarehouseId, query.SupplierId, query.PurchaseOrderId,
                Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
