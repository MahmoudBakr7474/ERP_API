using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Contracts.ProductTaxes;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;

public interface IProductTaxReadStore
{
    /// <summary>Active classifications, most recently changed first; one product's or one treatment's when filtered.</summary>
    Task<PagedResult<ProductTaxResponse>> ListAsync(ProductTaxFilter filter, int page, int pageSize, CancellationToken ct);
}
