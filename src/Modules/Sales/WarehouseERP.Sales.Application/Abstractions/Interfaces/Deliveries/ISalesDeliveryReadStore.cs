using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Deliveries;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;

public interface ISalesDeliveryReadStore
{
    Task<SalesDeliveryDetailsResponse?> GetAsync(Guid deliveryId, CancellationToken ct);

    Task<PagedResult<SalesDeliverySummaryResponse>> ListAsync(SalesDeliveryFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>What posting answered, for a retry with the same idempotency key; null unless it is posted.</summary>
    Task<SalesDeliveryPostedResponse?> FindPostedAsync(Guid deliveryId, CancellationToken ct);
}
