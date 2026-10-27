using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;

// The unit is added through its product (the domain refuses an inactive product, a unit it already has, and a split
// of a base unit that can't be split). One save point; the database still guards the product/unit pair.
public sealed partial class AddProductUnitHandler(IProductRepository products, ILogger<AddProductUnitHandler> logger)
    : ICommandHandler<AddProductUnitCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddProductUnitCommand command, CancellationToken ct)
    {
        var product = await products.GetWithUnitsForUpdateAsync(command.ProductId, ct);
        if (product is null)
            return ProductErrors.NotFound;
        if (await products.GetUnitStateAsync(command.UnitId, ct) is not { IsActive: true })
            return UnitErrors.NotActive;
        var baseUnit = await products.GetUnitStateAsync(product.BaseUnitId, ct);

        var added = product.AddUnit(command.UnitId, command.ConversionFactor, baseUnit?.AllowsFraction ?? false);
        if (added.IsFailure)
            return added.Error;
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ProductUnitAdded(logger, added.Value.Id, product.Id, command.UnitId, command.ConversionFactor);
        return added.Value.Id;
    }

    [LoggerMessage(EventId = 3010, Level = LogLevel.Information,
        Message = "Product unit {ProductUnitId} added to product {ProductId}: unit {UnitId} x {ConversionFactor}")]
    private static partial void ProductUnitAdded(ILogger logger, Guid productUnitId, Guid productId, Guid unitId, decimal conversionFactor);
}
