using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.GetProduct;

public sealed class GetProductHandler(IProductReadStore products) : IQueryHandler<GetProductQuery, ProductDetailsResponse>
{
    public async Task<Result<ProductDetailsResponse>> HandleAsync(GetProductQuery query, CancellationToken ct) =>
        await products.GetAsync(query.ProductId, ct) is { } product ? product : ProductErrors.NotFound;
}
