using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.LookupProduct;

// What a till or a sales screen gets when it scans: only an active barcode on an active unit of an active product.
public sealed class LookupProductHandler(IProductReadStore products) : IQueryHandler<LookupProductQuery, ProductLookupResponse>
{
    public async Task<Result<ProductLookupResponse>> HandleAsync(LookupProductQuery query, CancellationToken ct) =>
        await products.FindByBarcodeAsync(query.Barcode, ct) is { } item ? item : ProductBarcodeErrors.NoSellableItem;
}
