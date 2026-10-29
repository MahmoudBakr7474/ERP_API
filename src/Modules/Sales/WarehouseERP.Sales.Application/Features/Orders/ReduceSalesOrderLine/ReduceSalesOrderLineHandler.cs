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

namespace WarehouseERP.Sales.Application.Features.Orders.ReduceSalesOrderLine;

// A confirmed line's quantity goes down by what will not be delivered (never below what was delivered), and Inventory
// releases that much of the line's reservation in the same transaction. Its price and amounts stay as agreed; the closed
// quantity shows what was taken off. Returns the new row version.
public sealed partial class ReduceSalesOrderLineHandler(
    ITransactionRunner transactions, ISalesOrderRepository orders, IStockReservations reservations, ICurrentUser currentUser,
    TimeProvider clock, ILogger<ReduceSalesOrderLineHandler> logger)
    : ICommandHandler<ReduceSalesOrderLineCommand, string>
{
    public async Task<Result<string>> HandleAsync(ReduceSalesOrderLineCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ReduceAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> ReduceAsync(ReduceSalesOrderLineCommand command, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(command.OrderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var reduced = order.ReduceLine(command.LineId, command.Quantity, command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (reduced.IsFailure)
            return reduced.Error;
        var released = await OrderReleases.ReleaseAsync(reservations, order,
            [new OrderLineQuantity(command.LineId, command.Quantity)], command.Reason, ct);
        if (released.IsFailure)
            return released.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        LineReduced(logger, order.Id, command.LineId, command.Quantity);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7037, Level = LogLevel.Information, Message = "Sales order {OrderId} line {LineId} reduced by {Quantity}")]
    private static partial void LineReduced(ILogger logger, Guid orderId, Guid lineId, decimal quantity);
}
