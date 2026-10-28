using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.SubmitPurchaseOrder;

// A draft goes to the approvers when the policy it copied needs approval; otherwise it is approved and numbered at once,
// in one transaction with its number. Either way its supplier must still be active. Returns the new row version.
public sealed partial class SubmitPurchaseOrderHandler(
    ITransactionRunner transactions, IPurchaseOrderRepository orders, PurchasingReferences references, DocumentNumbering numbering,
    ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitPurchaseOrderHandler> logger)
    : ICommandHandler<SubmitPurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitPurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => SubmitAsync(command.PurchaseOrderId, expected, token), ct);
    }

    private async Task<Result<string>> SubmitAsync(Guid orderId, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(orderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;
        if (order.Status != PurchaseOrderStatus.Draft)
            return PurchaseOrderDomainErrors.NotDraft;
        var supplier = await references.ActiveSupplierAsync(order.SupplierId, ct);
        if (supplier.IsFailure)
            return supplier.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        Result done;
        if (order.RequiresApproval)
            done = order.Submit(currentUser.UserId, now);
        else
        {
            var number = await numbering.NextAsync(PurchasingDocumentType.PurchaseOrder, order.BranchId, ct);
            if (number.IsFailure)
                return number.Error;
            done = order.ApproveDirectly(number.Value, currentUser.UserId, now);
        }
        if (done.IsFailure)
            return done.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderSubmitted(logger, order.Id, order.Status);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6012, Level = LogLevel.Information, Message = "Purchase order {OrderId} submitted: now {Status}")]
    private static partial void OrderSubmitted(ILogger logger, Guid orderId, PurchaseOrderStatus status);
}
