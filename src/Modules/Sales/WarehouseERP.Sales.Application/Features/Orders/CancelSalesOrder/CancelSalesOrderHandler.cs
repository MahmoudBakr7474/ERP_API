using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.CancelSalesOrder;

// An order nothing was delivered from is abandoned; a confirmed one gives its whole reservation back to Inventory in the
// same transaction. Returns the new row version.
public sealed partial class CancelSalesOrderHandler(
    ITransactionRunner transactions, ISalesOrderRepository orders, IStockReservations reservations, ICurrentUser currentUser,
    TimeProvider clock, ILogger<CancelSalesOrderHandler> logger)
    : ICommandHandler<CancelSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => CancelAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> CancelAsync(CancelSalesOrderCommand command, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(command.OrderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var cancelled = order.Cancel(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (cancelled.IsFailure)
            return cancelled.Error;
        var released = await OrderReleases.ReleaseAsync(reservations, order,
            cancelled.Value.Select(l => new OrderLineQuantity(l.Id, l.RemainingQuantity)).ToList(),
            command.Reason ?? "Order cancelled", ct);
        if (released.IsFailure)
            return released.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderCancelled(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7035, Level = LogLevel.Information, Message = "Sales order {OrderId} cancelled")]
    private static partial void OrderCancelled(ILogger logger, Guid orderId);
}
