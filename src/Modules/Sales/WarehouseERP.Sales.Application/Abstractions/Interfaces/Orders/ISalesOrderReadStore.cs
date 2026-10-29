using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Orders;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;

public interface ISalesOrderReadStore
{
    Task<SalesOrderDetailsResponse?> GetAsync(Guid orderId, CancellationToken ct);

    Task<PagedResult<SalesOrderSummaryResponse>> ListAsync(SalesOrderFilter filter, int page, int pageSize, CancellationToken ct);
}
