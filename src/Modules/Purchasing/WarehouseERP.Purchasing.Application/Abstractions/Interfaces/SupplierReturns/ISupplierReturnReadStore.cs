using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;

public interface ISupplierReturnReadStore
{
    Task<SupplierReturnDetailsResponse?> GetAsync(Guid returnId, CancellationToken ct);

    Task<PagedResult<SupplierReturnSummaryResponse>> ListAsync(SupplierReturnFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What approving answered, for a retry with the same Idempotency-Key.</summary>
    Task<SupplierReturnPostedResponse?> FindPostedAsync(Guid returnId, CancellationToken ct);
}
