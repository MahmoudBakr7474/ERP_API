using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.ApproveSalesOrder;

// An approver (endpoint permission, warehouse scope), its creator included (CLAUDE.md), confirms a submitted order in one
// transaction: the customer must still be active and not on a blocking hold, then it is numbered and Inventory reserves
// every line or nothing. Returns the new row version.
public sealed partial class ApproveSalesOrderHandler(
    ITransactionRunner transactions, ISalesOrderRepository orders, OrderConfirmation confirmation, ICurrentUser currentUser,
    TimeProvider clock, ILogger<ApproveSalesOrderHandler> logger)
    : ICommandHandler<ApproveSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(ApproveSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command.OrderId, expected, token), ct);
    }

    private async Task<Result<string>> ApproveAsync(Guid orderId, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(orderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var allowed = order.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var checkedOrder = await confirmation.CheckAsync(order, approving: true, ct);
        if (checkedOrder.IsFailure)
            return checkedOrder.Error;
        var confirmed = await confirmation.ConfirmAsync(order, approving: true, currentUser.UserId, clock.GetUtcNow().UtcDateTime, ct);
        if (confirmed.IsFailure)
            return confirmed.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderApproved(logger, order.Id, order.Number!);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7033, Level = LogLevel.Information, Message = "Sales order {OrderId} approved and confirmed as {Number}")]
    private static partial void OrderApproved(ILogger logger, Guid orderId, string number);
}
