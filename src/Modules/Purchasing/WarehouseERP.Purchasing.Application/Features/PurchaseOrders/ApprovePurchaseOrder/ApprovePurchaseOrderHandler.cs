using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.ApprovePurchaseOrder;

// An approver (endpoint permission, warehouse scope), its creator included, approves a submitted order to a supplier that
// is still active: it is numbered in the same transaction and its goods may be received. Returns the new row version.
public sealed partial class ApprovePurchaseOrderHandler(
    ITransactionRunner transactions, IPurchaseOrderRepository orders, PurchasingReferences references, DocumentNumbering numbering,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ApprovePurchaseOrderHandler> logger)
    : ICommandHandler<ApprovePurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(ApprovePurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command.PurchaseOrderId, expected, token), ct);
    }

    private async Task<Result<string>> ApproveAsync(Guid orderId, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(orderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;
        var allowed = order.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var supplier = await references.ActiveSupplierAsync(order.SupplierId, ct);
        if (supplier.IsFailure)
            return supplier.Error;
        var number = await numbering.NextAsync(PurchasingDocumentType.PurchaseOrder, order.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var approved = order.Approve(number.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (approved.IsFailure)
            return approved.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderApproved(logger, order.Id, number.Value.Number);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6013, Level = LogLevel.Information, Message = "Purchase order {OrderId} approved as {Number}")]
    private static partial void OrderApproved(ILogger logger, Guid orderId, string number);
}
