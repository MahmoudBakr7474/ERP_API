using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.CancelSalesDelivery;

// A draft picking list is abandoned; stock and the order's reservation were never touched. Returns the new row version.
public sealed partial class CancelSalesDeliveryHandler(
    ISalesDeliveryRepository deliveries, ICurrentUser currentUser, TimeProvider clock, ILogger<CancelSalesDeliveryHandler> logger)
    : ICommandHandler<CancelSalesDeliveryCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSalesDeliveryCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await DeliveryAccess.AuthorizeAsync(deliveries, currentUser, command.DeliveryId, ct);
        if (access.IsFailure)
            return access.Error;
        var delivery = await deliveries.GetForUpdateAsync(command.DeliveryId, expected, ct);
        if (delivery is null)
            return DeliveryErrors.NotFound;
        var cancelled = delivery.Cancel(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var saved = await deliveries.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        DeliveryCancelled(logger, delivery.Id);
        return Convert.ToBase64String(delivery.RowVersion);
    }

    [LoggerMessage(EventId = 7042, Level = LogLevel.Information, Message = "Sales delivery {DeliveryId} cancelled")]
    private static partial void DeliveryCancelled(ILogger logger, Guid deliveryId);
}
