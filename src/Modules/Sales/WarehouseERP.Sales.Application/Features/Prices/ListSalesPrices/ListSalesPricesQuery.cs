using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Sales.Application.Features.Prices.ListSalesPrices;

public sealed record ListSalesPricesQuery(int Page, int? PageSize, Guid? ProductId, Guid? ProductUnitId) : IPagedQuery;
