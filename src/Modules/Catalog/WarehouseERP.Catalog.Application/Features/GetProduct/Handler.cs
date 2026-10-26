using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.GetProduct;

public sealed class GetProductHandler(IProductReadStore products) : IQueryHandler<GetProductQuery, ProductDetails>
{
    public async Task<Result<ProductDetails>> HandleAsync(GetProductQuery query, CancellationToken ct) =>
        await products.GetAsync(query.ProductId, ct) is { } product ? product : CatalogErrors.ProductNotFound;
}
