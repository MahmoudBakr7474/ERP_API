using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Contracts.Counts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;

public interface IStockCountReadStore
{
    Task<StockCountDetailsResponse?> GetAsync(Guid countId, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<PagedResult<StockCountSummaryResponse>> ListAsync(StockCountFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What a completed count's approval posted, to answer a retried request the way it was answered.</summary>
    Task<StockCountApprovalResponse?> FindApprovedAsync(Guid countId, CancellationToken ct);
}
