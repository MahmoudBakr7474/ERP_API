using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Contracts.Valuation;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Valuation;

public interface IValuationReadStore
{
    Task<PagedResult<InventoryValuationResponse>> ListAsync(Guid? productId, bool includeZero, int page, int pageSize, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<PagedResult<InventoryCostEntryResponse>> ListEntriesAsync(Guid productId, int page, int pageSize, CancellationToken ct);
}
