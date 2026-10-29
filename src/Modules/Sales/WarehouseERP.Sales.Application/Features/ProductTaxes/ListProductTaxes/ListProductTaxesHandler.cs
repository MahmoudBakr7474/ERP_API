using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Application.Contracts.ProductTaxes;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.ListProductTaxes;

// Tax classifications are company-wide, like the price list.
public sealed class ListProductTaxesHandler(IProductTaxReadStore taxes, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListProductTaxesQuery, PagedResult<ProductTaxResponse>>
{
    public async Task<Result<PagedResult<ProductTaxResponse>>> HandleAsync(ListProductTaxesQuery query, CancellationToken ct) =>
        await taxes.ListAsync(new ProductTaxFilter(query.ProductId, query.TaxTreatment), query.Page, query.ResolvePageSize(pagination.Value), ct);
}
