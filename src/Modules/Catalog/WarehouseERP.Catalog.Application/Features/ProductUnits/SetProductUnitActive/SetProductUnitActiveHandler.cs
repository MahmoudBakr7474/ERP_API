using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.ProductUnits.SetProductUnitActive;

// The base unit stays active (the domain refuses); a product unit comes back only while its unit is active.
public sealed partial class SetProductUnitActiveHandler(IProductRepository products, ILogger<SetProductUnitActiveHandler> logger)
    : ICommandHandler<SetProductUnitActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetProductUnitActiveCommand command, CancellationToken ct)
    {
        var product = await products.GetWithUnitsForUpdateAsync(command.ProductId, ct);
        if (product is null)
            return ProductErrors.NotFound;
        var productUnit = product.Units.SingleOrDefault(x => x.Id == command.ProductUnitId);
        if (productUnit is null)
            return ProductUnitErrors.NotFound;
        if (command.IsActive && !await products.IsUnitActiveAsync(productUnit.UnitId, ct))
            return UnitErrors.NotActive;

        var changed = product.SetUnitActive(productUnit, command.IsActive);
        if (changed.IsFailure)
            return changed.Error;
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, productUnit.Id, product.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 3011, Level = LogLevel.Information,
        Message = "Product unit {ProductUnitId} of product {ProductId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid productUnitId, Guid productId, bool isActive);
}
