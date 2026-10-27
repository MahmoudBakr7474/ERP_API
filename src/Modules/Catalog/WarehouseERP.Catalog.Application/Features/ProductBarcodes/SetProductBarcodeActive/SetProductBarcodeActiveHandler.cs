using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.ProductBarcodes.SetProductBarcodeActive;

// A retired barcode stops scanning but keeps its value reserved; it comes back only while its product unit is active.
public sealed partial class SetProductBarcodeActiveHandler(
    IProductRepository products, ILogger<SetProductBarcodeActiveHandler> logger)
    : ICommandHandler<SetProductBarcodeActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetProductBarcodeActiveCommand command, CancellationToken ct)
    {
        var product = await products.GetWithUnitsForUpdateAsync(command.ProductId, ct);
        if (product is null)
            return ProductErrors.NotFound;
        var barcode = product.Units.SelectMany(x => x.Barcodes).SingleOrDefault(x => x.Id == command.BarcodeId);
        if (barcode is null)
            return ProductBarcodeErrors.NotFound;

        var changed = product.SetBarcodeActive(barcode, command.IsActive);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, barcode.Id, product.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 3013, Level = LogLevel.Information,
        Message = "Barcode {BarcodeId} of product {ProductId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid barcodeId, Guid productId, bool isActive);
}
