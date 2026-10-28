using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.RejectPurchaseOrder;

// An approver sends a submitted order back to draft with a reason. Returns the new row version.
public sealed partial class RejectPurchaseOrderHandler(
    IPurchaseOrderRepository orders, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectPurchaseOrderHandler> logger)
    : ICommandHandler<RejectPurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectPurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;

        var rejected = order.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderRejected(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6014, Level = LogLevel.Information, Message = "Purchase order {OrderId} rejected")]
    private static partial void OrderRejected(ILogger logger, Guid orderId);
}
