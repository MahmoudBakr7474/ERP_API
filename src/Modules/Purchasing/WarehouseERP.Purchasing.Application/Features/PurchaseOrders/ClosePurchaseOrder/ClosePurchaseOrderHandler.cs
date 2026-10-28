using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ClosePurchaseOrder;

// After a partial receipt, what was not received is closed with a reason: nothing more is received. What arrived may
// still be invoiced and returned. Returns the new row version.
public sealed partial class ClosePurchaseOrderHandler(
    IPurchaseOrderRepository orders, ICurrentUser currentUser, TimeProvider clock, ILogger<ClosePurchaseOrderHandler> logger)
    : ICommandHandler<ClosePurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(ClosePurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;

        var closed = order.Close(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (closed.IsFailure)
            return closed.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderClosed(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6016, Level = LogLevel.Information, Message = "Purchase order {OrderId} closed")]
    private static partial void OrderClosed(ILogger logger, Guid orderId);
}
