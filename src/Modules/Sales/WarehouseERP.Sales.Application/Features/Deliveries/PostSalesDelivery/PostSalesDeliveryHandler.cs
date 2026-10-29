using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Sales.Application.Contracts.Deliveries;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Deliveries.PostSalesDelivery;

// Posting is one transaction: the request's key is locked and looked up (a retry gets the first answer), the delivery is
// locked at the version the user saw, then its order (two deliveries, invoices or returns of one order wait for each other
// here, so remaining quantities can't be over-delivered), the picks are checked again against what remains, a credit hold
// blocks it when the settings say so, the delivery is numbered, and Inventory posts it as a SalesDelivery: each pick
// leaves its location and its order line's reservation at the moving average, with its own locks and checks (the stock must
// physically be in the picked location). The delivery, the order, the stock document, the reservation and the idempotency
// record commit together or not at all.
public sealed partial class PostSalesDeliveryHandler(
    ITransactionRunner transactions, IdempotentRequests requests, ISalesDeliveryRepository deliveries, ISalesOrderRepository orders,
    ISalesDeliveryReadStore readStore, ISalesSettingsRepository settings, SalesReferences references, DocumentNumbering numbering,
    ISalesStockPosting stock, ICurrentUser currentUser, TimeProvider clock, ILogger<PostSalesDeliveryHandler> logger)
    : ICommandHandler<PostSalesDeliveryCommand, SalesDeliveryPostedResponse>
{
    private const string Operation = "PostSalesDelivery";

    public async Task<Result<SalesDeliveryPostedResponse>> HandleAsync(PostSalesDeliveryCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await DeliveryAccess.AuthorizeAsync(deliveries, currentUser, command.DeliveryId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => PostAsync(command, expected, token), ct);
    }

    private async Task<Result<SalesDeliveryPostedResponse>> PostAsync(PostSalesDeliveryCommand command, byte[] expected, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.DeliveryId, expected, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } postedId)
            return await readStore.FindPostedAsync(postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at delivery {postedId}, which is not posted.");

        var delivery = await deliveries.GetForUpdateAsync(command.DeliveryId, expected, ct);
        if (delivery is null)
            return DeliveryErrors.NotFound;
        if (delivery.Status != SalesDeliveryStatus.Draft)
            return SalesDeliveryDomainErrors.NotDraft;
        var order = await orders.GetForUpdateAsync(delivery.OrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Delivery {delivery.Id} points at order {delivery.OrderId}, which does not exist.");
        var quantities = delivery.OrderQuantities();
        var deliverable = order.CanDeliver(quantities);
        if (deliverable.IsFailure)
            return deliverable.Error;
        var customer = await references.CustomerAsync(order.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        if (customer.Value.IsOnCreditHold && (await settings.GetAsync(ct)).CreditHoldBlocksDeliveries)
            return CustomerErrors.OnCreditHold;
        var number = await numbering.NextAsync(SalesDocumentType.Delivery, delivery.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var lines = delivery.Lines.OrderBy(l => l.LineNumber).ToList();
        var posted = await stock.PostDeliveryAsync(new SalesDeliveryStockRequest(
            delivery.Id, number.Value.Number, delivery.WarehouseId, order.Id, delivery.Notes,
            lines.Select(l => new SalesDeliveryStockLine(l.OrderLineId, l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList()), ct);
        if (!posted.Succeeded)
            return StockErrors.Refused(posted);

        var now = clock.GetUtcNow().UtcDateTime;
        delivery.MarkPosted(number.Value, posted.DocumentId!.Value, posted.Number!,
            posted.Lines.Select(l => (l.StockLineId, l.CostAmount)).ToList(), userId, now);
        var recorded = order.RecordDelivery(quantities, number.Value.Number, userId, now);
        if (recorded.IsFailure)
            return recorded.Error;
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, delivery.Id, userId, now);

        var saved = await deliveries.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        DeliveryPosted(logger, delivery.Id, number.Value.Number, order.Id, posted.Number!);
        return new SalesDeliveryPostedResponse(delivery.Id, number.Value.Number, posted.DocumentId.Value, posted.Number!, delivery.CostAmount, now);
    }

    [LoggerMessage(EventId = 7043, Level = LogLevel.Information,
        Message = "Sales delivery {DeliveryId} ({Number}) of order {OrderId} posted to stock as {StockDocumentNumber}")]
    private static partial void DeliveryPosted(ILogger logger, Guid deliveryId, string number, Guid orderId, string stockDocumentNumber);
}
