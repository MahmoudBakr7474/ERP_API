using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Prices.RemoveSalesPrice;

// Removing a price deactivates it (its history stays): new lines of that unit then need an authorized manual price.
public sealed partial class RemoveSalesPriceHandler(ISalesPriceRepository prices, ILogger<RemoveSalesPriceHandler> logger)
    : ICommandHandler<RemoveSalesPriceCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(RemoveSalesPriceCommand command, CancellationToken ct)
    {
        var price = await prices.GetForUpdateAsync(command.ProductUnitId, ct);
        if (price is not { IsActive: true })
            return PriceErrors.NotFound;
        price.Deactivate();
        var saved = await prices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PriceRemoved(logger, command.ProductUnitId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 7012, Level = LogLevel.Information, Message = "Sales price of product unit {ProductUnitId} removed")]
    private static partial void PriceRemoved(ILogger logger, Guid productUnitId);
}
