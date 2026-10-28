using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Contracts.Transfers;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;

public interface IStockTransferReadStore
{
    Task<StockTransferDetailsResponse?> GetAsync(Guid transferId, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<PagedResult<StockTransferSummaryResponse>> ListAsync(StockTransferFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What a dispatch or receipt request posted, to answer its retry the way it was answered.</summary>
    Task<StockTransferPostedResponse?> FindPostedAsync(Guid transferId, Guid documentId, CancellationToken ct);
}
