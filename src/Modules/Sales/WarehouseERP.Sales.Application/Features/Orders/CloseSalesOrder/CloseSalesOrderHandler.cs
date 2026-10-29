using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.CloseSalesOrder;

// After a partial delivery, what was not delivered is closed with a reason and its reservation goes back to Inventory in
// the same transaction; delivered goods are still invoiced and returned. Returns the new row version.
public sealed partial class CloseSalesOrderHandler(
    ITransactionRunner transactions, ISalesOrderRepository orders, IStockReservations reservations, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CloseSalesOrderHandler> logger)
    : ICommandHandler<CloseSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(CloseSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => CloseAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> CloseAsync(CloseSalesOrderCommand command, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(command.OrderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var closed = order.Close(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (closed.IsFailure)
            return closed.Error;
        var released = await OrderReleases.ReleaseAsync(reservations, order, closed.Value, command.Reason, ct);
        if (released.IsFailure)
            return released.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderClosed(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7036, Level = LogLevel.Information, Message = "Sales order {OrderId} closed")]
    private static partial void OrderClosed(ILogger logger, Guid orderId);
}
