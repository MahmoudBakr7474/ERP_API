using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.SubmitSalesOrder;

// One transaction with the order locked. The sales settings decide now whether the draft needs an approver (policy,
// discount threshold, manual or below-list prices, a credit hold that asks for approval); otherwise it is confirmed at
// once: numbered, and Inventory reserves every line or nothing (a shortage refuses it and rolls everything back). A credit
// hold that blocks confirmation refuses it. Returns the new row version.
public sealed partial class SubmitSalesOrderHandler(
    ITransactionRunner transactions, ISalesOrderRepository orders, OrderConfirmation confirmation, ICurrentUser currentUser,
    TimeProvider clock, ILogger<SubmitSalesOrderHandler> logger)
    : ICommandHandler<SubmitSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => SubmitAsync(command.OrderId, expected, token), ct);
    }

    private async Task<Result<string>> SubmitAsync(Guid orderId, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(orderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        if (order.Status != SalesOrderStatus.Draft)
            return SalesOrderDomainErrors.NotDraft;
        var reasons = await confirmation.CheckAsync(order, approving: false, ct);
        if (reasons.IsFailure)
            return reasons.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var done = reasons.Value.Count > 0
            ? order.Submit(reasons.Value, currentUser.UserId, now)
            : await confirmation.ConfirmAsync(order, approving: false, currentUser.UserId, now, ct);
        if (done.IsFailure)
            return done.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderSubmitted(logger, order.Id, order.Status);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7032, Level = LogLevel.Information, Message = "Sales order {OrderId} submitted: now {Status}")]
    private static partial void OrderSubmitted(ILogger logger, Guid orderId, SalesOrderStatus status);
}
