using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.Prices;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;

public interface ISalesPriceReadStore
{
    /// <summary>Active prices, most recently changed first; one product's or one unit's when filtered.</summary>
    Task<PagedResult<SalesPriceResponse>> ListAsync(SalesPriceFilter filter, int page, int pageSize, CancellationToken ct);
}
