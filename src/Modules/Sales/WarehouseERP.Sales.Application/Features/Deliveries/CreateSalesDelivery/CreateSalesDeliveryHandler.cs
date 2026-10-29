using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.CreateSalesDelivery;

// A draft delivery (picking list) for an open order in the user's scope: each pick names an order line, a quantity in its
// unit and a location of the order's warehouse, all picks of a line together at most what remains to deliver now. Stock is
// checked when it is posted, location by location.
public sealed partial class CreateSalesDeliveryHandler(
    ISalesDeliveryRepository deliveries, ISalesOrderRepository orders, SalesReferences references, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CreateSalesDeliveryHandler> logger)
    : ICommandHandler<CreateSalesDeliveryCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSalesDeliveryCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await orders.FindWarehouseAsync(command.OrderId, ct), OrderErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.OrderId, expectedRowVersion: null, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var lines = await DeliveryLines.ResolveAsync(references, order, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var created = SalesDelivery.CreateDraft(order, command.Notes, lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        deliveries.Add(created.Value);
        var saved = await deliveries.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        DeliveryCreated(logger, created.Value.Id, order.Id);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7040, Level = LogLevel.Information, Message = "Sales delivery {DeliveryId} drafted for order {OrderId}")]
    private static partial void DeliveryCreated(ILogger logger, Guid deliveryId, Guid orderId);
}
