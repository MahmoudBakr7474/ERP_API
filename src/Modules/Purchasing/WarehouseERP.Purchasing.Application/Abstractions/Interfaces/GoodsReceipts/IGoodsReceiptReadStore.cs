using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;

public interface IGoodsReceiptReadStore
{
    Task<GoodsReceiptDetailsResponse?> GetAsync(Guid receiptId, CancellationToken ct);

    Task<PagedResult<GoodsReceiptSummaryResponse>> ListAsync(GoodsReceiptFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What receiving answered, for a retry with the same Idempotency-Key.</summary>
    Task<GoodsReceiptPostedResponse?> FindPostedAsync(Guid receiptId, CancellationToken ct);
}
