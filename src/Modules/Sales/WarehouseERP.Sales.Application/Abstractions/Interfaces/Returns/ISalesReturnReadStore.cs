using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Returns;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;

public interface ISalesReturnReadStore
{
    Task<SalesReturnDetailsResponse?> GetAsync(Guid returnId, CancellationToken ct);

    Task<PagedResult<SalesReturnSummaryResponse>> ListAsync(SalesReturnFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What approving answered, for a retry with the same idempotency key; null unless it is approved.</summary>
    Task<SalesReturnApprovedResponse?> FindApprovedAsync(Guid returnId, CancellationToken ct);
}
