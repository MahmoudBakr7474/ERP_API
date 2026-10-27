using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;

// The barcode is added through its product (the domain checks the format and that the product and its unit are
// active). One save point; the pre-check gives a clear error and the database still keeps every value unique,
// retired ones included.
public sealed partial class AddProductBarcodeHandler(IProductRepository products, ILogger<AddProductBarcodeHandler> logger)
    : ICommandHandler<AddProductBarcodeCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddProductBarcodeCommand command, CancellationToken ct)
    {
        var product = await products.GetWithUnitsForUpdateAsync(command.ProductId, ct);
        if (product is null)
            return ProductErrors.NotFound;
        var productUnit = product.Units.SingleOrDefault(x => x.Id == command.ProductUnitId);
        if (productUnit is null)
            return ProductUnitErrors.NotFound;
        if (await products.BarcodeExistsAsync(command.Barcode, ct))
            return ProductBarcodeErrors.Taken;

        var added = product.AddBarcode(productUnit, command.Barcode, command.BarcodeType);
        if (added.IsFailure)
            return added.Error;
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        BarcodeAdded(logger, added.Value.Id, product.Id, productUnit.Id);
        return added.Value.Id;
    }

    [LoggerMessage(EventId = 3012, Level = LogLevel.Information,
        Message = "Barcode {BarcodeId} added to product {ProductId}, product unit {ProductUnitId}")]
    private static partial void BarcodeAdded(ILogger logger, Guid barcodeId, Guid productId, Guid productUnitId);
}
