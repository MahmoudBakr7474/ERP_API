using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.UpdateSalesDelivery;

// Only a draft, only from the version the editor read; the picks are checked again against what remains. Returns the new
// row version.
public sealed partial class UpdateSalesDeliveryHandler(
    ISalesDeliveryRepository deliveries, ISalesOrderRepository orders, SalesReferences references, ICurrentUser currentUser,
    ILogger<UpdateSalesDeliveryHandler> logger)
    : ICommandHandler<UpdateSalesDeliveryCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesDeliveryCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await DeliveryAccess.AuthorizeAsync(deliveries, currentUser, command.DeliveryId, ct);
        if (access.IsFailure)
            return access.Error;
        var delivery = await deliveries.GetForUpdateAsync(command.DeliveryId, expected, ct);
        if (delivery is null)
            return DeliveryErrors.NotFound;
        var order = await orders.GetForUpdateAsync(delivery.OrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Delivery {delivery.Id} points at order {delivery.OrderId}, which does not exist.");
        var lines = await DeliveryLines.ResolveAsync(references, order, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = delivery.ReplaceDraft(order, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await deliveries.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        DeliveryUpdated(logger, delivery.Id);
        return Convert.ToBase64String(delivery.RowVersion);
    }

    [LoggerMessage(EventId = 7041, Level = LogLevel.Information, Message = "Sales delivery {DeliveryId} updated")]
    private static partial void DeliveryUpdated(ILogger logger, Guid deliveryId);
}
