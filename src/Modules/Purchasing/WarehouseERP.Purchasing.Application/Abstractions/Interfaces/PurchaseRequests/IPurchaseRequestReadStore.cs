using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;

public interface IPurchaseRequestReadStore
{
    Task<PurchaseRequestDetailsResponse?> GetAsync(Guid requestId, CancellationToken ct);

    Task<PagedResult<PurchaseRequestSummaryResponse>> ListAsync(PurchaseRequestFilter filter, int page, int pageSize, CancellationToken ct);
}
