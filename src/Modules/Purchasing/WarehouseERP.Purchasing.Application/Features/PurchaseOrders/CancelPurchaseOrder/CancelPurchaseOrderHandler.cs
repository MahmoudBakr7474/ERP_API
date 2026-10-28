using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.CancelPurchaseOrder;

// An order nothing was received against is abandoned; what it took from purchase requests goes back to them in the same
// transaction. Returns the new row version.
public sealed partial class CancelPurchaseOrderHandler(
    ITransactionRunner transactions, IPurchaseOrderRepository orders, IPurchaseRequestRepository requests,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CancelPurchaseOrderHandler> logger)
    : ICommandHandler<CancelPurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelPurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => CancelAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> CancelAsync(CancelPurchaseOrderCommand command, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;

        var drawn = RequestDraws.Of(order.Lines);
        var cancelled = order.Cancel(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var released = await RequestDraws.MoveAsync(requests, order.WarehouseId, drawn, new Dictionary<Guid, RequestDraw>(), ct);
        if (released.IsFailure)
            return released.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderCancelled(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6015, Level = LogLevel.Information, Message = "Purchase order {OrderId} cancelled")]
    private static partial void OrderCancelled(ILogger logger, Guid orderId);
}
