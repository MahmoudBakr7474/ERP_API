using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.SetProductActive;

// A product comes back only while its base unit is active, so an active product never stands on a retired unit.
public sealed partial class SetProductActiveHandler(IProductRepository products, ILogger<SetProductActiveHandler> logger)
    : ICommandHandler<SetProductActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetProductActiveCommand command, CancellationToken ct)
    {
        var product = await products.GetForUpdateAsync(command.ProductId, expectedRowVersion: null, ct);
        if (product is null)
            return ProductErrors.NotFound;
        if (command.IsActive && !await products.IsUnitActiveAsync(product.BaseUnitId, ct))
            return UnitErrors.NotActive;

        if (command.IsActive) product.Activate();
        else product.Deactivate();
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, product.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 3009, Level = LogLevel.Information, Message = "Product {ProductId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid productId, bool isActive);
}
