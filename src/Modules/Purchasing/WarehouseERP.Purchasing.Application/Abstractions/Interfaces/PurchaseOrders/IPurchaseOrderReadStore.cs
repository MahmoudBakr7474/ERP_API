using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.PurchaseOrders;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;

public interface IPurchaseOrderReadStore
{
    Task<PurchaseOrderDetailsResponse?> GetAsync(Guid orderId, CancellationToken ct);

    Task<PagedResult<PurchaseOrderSummaryResponse>> ListAsync(PurchaseOrderFilter filter, int page, int pageSize, CancellationToken ct);
}
