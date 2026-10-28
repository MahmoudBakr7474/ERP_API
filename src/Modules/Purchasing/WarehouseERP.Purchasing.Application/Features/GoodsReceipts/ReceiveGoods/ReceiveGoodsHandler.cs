using System.Globalization;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Contracts.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.GoodsReceipts.ReceiveGoods;

// Receiving is one transaction: the request's key is locked and looked up (a retry gets the first answer), the order is
// locked (two receipts of one order wait for each other, so remaining quantities can't be over-received), the order's
// lines take the quantities (never beyond what was ordered), the receipt is numbered, and Inventory posts it as a
// PurchaseReceipt at the order's net unit prices through IPurchaseStockPosting, with its own locks and checks. The
// receipt, the order, the stock document and the idempotency record commit together or not at all.
public sealed partial class ReceiveGoodsHandler(
    ITransactionRunner transactions, IdempotentRequests requests, IPurchaseOrderRepository orders, IGoodsReceiptRepository receipts,
    IGoodsReceiptReadStore readStore, PurchasingReferences references, DocumentNumbering numbering, IPurchaseStockPosting stock,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ReceiveGoodsHandler> logger)
    : ICommandHandler<ReceiveGoodsCommand, GoodsReceiptPostedResponse>
{
    private const string Operation = "ReceiveGoods";

    public async Task<Result<GoodsReceiptPostedResponse>> HandleAsync(ReceiveGoodsCommand command, CancellationToken ct)
    {
        var access = DocumentAccess.Check(currentUser, await orders.FindWarehouseAsync(command.PurchaseOrderId, ct), PurchaseOrderErrors.NotFound);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ReceiveAsync(command, token), ct);
    }

    private async Task<Result<GoodsReceiptPostedResponse>> ReceiveAsync(ReceiveGoodsCommand command, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.PurchaseOrderId, null, userId, Content(command));
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } receiptId)
            return await readStore.FindPostedAsync(receiptId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at goods receipt {receiptId}, which does not exist.");

        var order = await orders.GetForUpdateAsync(command.PurchaseOrderId, expectedRowVersion: null, ct);
        if (order is null)
            return PurchaseOrderErrors.NotFound;
        var locations = await references.ResolveLocationsAsync(order.WarehouseId, command.Lines.Select(l => l.LocationId).ToList(), ct);
        if (locations.IsFailure)
            return locations.Error;
        var lines = command.Lines.Select((l, i) => new ReceiptLineInput(l.PurchaseOrderLineId, l.Quantity, locations.Value[i])).ToList();
        var number = await numbering.NextAsync(PurchasingDocumentType.GoodsReceipt, order.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var received = order.Receive(lines, number.Value.Number, userId, now);
        if (received.IsFailure)
            return received.Error;
        var receipt = GoodsReceipt.Create(order, number.Value, lines, command.SupplierDeliveryNote, command.Notes, userId, now);
        receipts.Add(receipt);
        var posted = await stock.PostReceiptAsync(new PurchaseStockRequest(
            receipt.Id, receipt.Number, order.WarehouseId, receipt.Notes, ApprovedByUserId: null,
            receipt.Lines.OrderBy(l => l.LineNumber)
                .Select(l => new PurchaseStockLine(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId, l.UnitCost))
                .ToList()), ct);
        if (!posted.Succeeded)
            return StockPostingErrors.Refused(posted);
        receipt.LinkStockDocument(posted.DocumentId!.Value, posted.Number!);
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, receipt.Id, userId, now);

        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        GoodsReceived(logger, receipt.Id, receipt.Number, order.Id, posted.Number!);
        return new GoodsReceiptPostedResponse(receipt.Id, receipt.Number, receipt.StockDocumentId, receipt.StockDocumentNumber, now);
    }

    // What makes a retry the same receipt: every line, its quantity and location, and the delivery note.
    private static string Content(ReceiveGoodsCommand command) =>
        string.Join(';', command.Lines.Select(l => string.Create(CultureInfo.InvariantCulture,
            $"{l.PurchaseOrderLineId:N}:{l.Quantity}:{l.LocationId:N}"))) + "|" + command.SupplierDeliveryNote + "|" + command.Notes;

    [LoggerMessage(EventId = 6020, Level = LogLevel.Information,
        Message = "Goods receipt {ReceiptId} ({Number}) of order {OrderId} posted to stock as {StockDocumentNumber}")]
    private static partial void GoodsReceived(ILogger logger, Guid receiptId, string number, Guid orderId, string stockDocumentNumber);
}
