using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.RejectSalesOrder;

// An approver sends a submitted order back to draft with a reason; nothing was reserved yet. Returns the new row version.
public sealed partial class RejectSalesOrderHandler(
    ISalesOrderRepository orders, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectSalesOrderHandler> logger)
    : ICommandHandler<RejectSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.OrderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var rejected = order.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderRejected(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7034, Level = LogLevel.Information, Message = "Sales order {OrderId} rejected")]
    private static partial void OrderRejected(ILogger logger, Guid orderId);
}
