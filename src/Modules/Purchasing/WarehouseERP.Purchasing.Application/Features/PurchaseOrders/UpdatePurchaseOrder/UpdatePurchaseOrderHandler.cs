using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseRequests;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.PurchaseOrders.UpdatePurchaseOrder;

// Only a draft changes, only from the version its editor read (409 otherwise), and only to an active supplier. What the
// old and new lines take from purchase requests moves in the same transaction. Returns the new row version.
public sealed partial class UpdatePurchaseOrderHandler(
    ITransactionRunner transactions, IPurchaseOrderRepository orders, IPurchaseRequestRepository requests,
    PurchasingReferences references, ICurrentUser currentUser, ILogger<UpdatePurchaseOrderHandler> logger)
    : ICommandHandler<UpdatePurchaseOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdatePurchaseOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.PurchaseOrderId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => UpdateAsync(command, expected, token), ct);
    }

    private async Task<Result<string>> UpdateAsync(UpdatePurchaseOrderCommand command, byte[] expected, CancellationToken ct)
    {
        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expected, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;
        if (order.Status != PurchaseOrderStatus.Draft)
            return PurchaseOrderDomainErrors.NotDraft;
        var supplier = await references.ActiveSupplierAsync(command.SupplierId, ct);
        if (supplier.IsFailure)
            return supplier.Error;
        var lines = await OrderLines.ResolveAsync(references, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var before = RequestDraws.Of(order.Lines);
        var replaced = order.ReplaceDraft(
            new OrderTerms(command.SupplierId, command.ExpectedDeliveryDate, command.SupplierReference, command.PaymentTermDays, command.Notes),
            lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var drawn = await RequestDraws.MoveAsync(requests, order.WarehouseId, before, RequestDraws.Of(order.Lines), ct);
        if (drawn.IsFailure)
            return drawn.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        OrderUpdated(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 6011, Level = LogLevel.Information, Message = "Purchase order {OrderId} draft updated")]
    private static partial void OrderUpdated(ILogger logger, Guid orderId);
}
