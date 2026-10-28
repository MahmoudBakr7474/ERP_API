using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.GoodsReceipts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.PurchaseOrders;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.SupplierReturns;
using WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.SupplierReturns.ApproveSupplierReturn;

// An approver (endpoint permission, warehouse scope), its creator included, approves a submitted return as one
// transaction: the key is locked and looked up (a retry gets the first answer); the return, its order and its receipt are
// locked in that order. The order splits each returned quantity between goods not invoiced yet and goods already invoiced
// (a pending credit claim at the invoiced price); the receipt line never gives back more than it brought in; the return is
// numbered; and Inventory issues the goods through a PurchaseReturn at the current moving average, refusing what is not
// physically there.
public sealed partial class ApproveSupplierReturnHandler(
    ITransactionRunner transactions, IdempotentRequests requests, ISupplierReturnRepository returns, IPurchaseOrderRepository orders,
    IGoodsReceiptRepository receipts, ISupplierReturnReadStore readStore, DocumentNumbering numbering, IPurchaseStockPosting stock,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ApproveSupplierReturnHandler> logger)
    : ICommandHandler<ApproveSupplierReturnCommand, SupplierReturnPostedResponse>
{
    private const string Operation = "ApproveSupplierReturn";

    public async Task<Result<SupplierReturnPostedResponse>> HandleAsync(ApproveSupplierReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.SupplierReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command, expected, token), ct);
    }

    private async Task<Result<SupplierReturnPostedResponse>> ApproveAsync(ApproveSupplierReturnCommand command, byte[] rowVersion, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.SupplierReturnId, rowVersion, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } postedId)
            return await readStore.FindPostedAsync(postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at return {postedId}, which is not posted.");

        var supplierReturn = await returns.GetForUpdateAsync(command.SupplierReturnId, expectedRowVersion: null, ct);
        if (supplierReturn is null)
            return SupplierReturnErrors.NotFound;
        if (!supplierReturn.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = supplierReturn.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var order = await orders.GetForUpdateAsync(supplierReturn.PurchaseOrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Return {supplierReturn.Id} points at order {supplierReturn.PurchaseOrderId}, which does not exist.");
        var receipt = await receipts.GetForUpdateAsync(supplierReturn.GoodsReceiptId, ct)
            ?? throw new InvalidOperationException($"Return {supplierReturn.Id} points at receipt {supplierReturn.GoodsReceiptId}, which does not exist.");
        var number = await numbering.NextAsync(PurchasingDocumentType.SupplierReturn, supplierReturn.BranchId, ct);
        if (number.IsFailure)
            return number.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var lines = supplierReturn.Lines.OrderBy(l => l.LineNumber).ToList();
        var fromReceipt = receipt.RecordReturn(lines);
        if (fromReceipt.IsFailure)
            return fromReceipt.Error;
        var splits = order.RecordReturn(lines.Select(l => new ReturnedQuantity(l.PurchaseOrderLineId, l.Quantity)).ToList(),
            number.Value.Number, userId, now);
        if (splits.IsFailure)
            return splits.Error;
        var posted = await stock.PostReturnAsync(new PurchaseStockRequest(
            supplierReturn.Id, number.Value.Number, supplierReturn.WarehouseId, supplierReturn.Reason, userId,
            lines.Select(l => new PurchaseStockLine(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId, UnitCost: null)).ToList()), ct);
        if (!posted.Succeeded)
            return StockPostingErrors.Refused(posted);
        supplierReturn.MarkPosted(number.Value, splits.Value, posted.DocumentId!.Value, posted.Number!, userId, now);
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, supplierReturn.Id, userId, now);
        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReturnPosted(logger, supplierReturn.Id, number.Value.Number, posted.Number!, supplierReturn.CreditClaimTotalAmount);
        return new SupplierReturnPostedResponse(supplierReturn.Id, number.Value.Number, posted.DocumentId.Value, posted.Number!,
            supplierReturn.CreditClaimTotalAmount, now);
    }

    [LoggerMessage(EventId = 6043, Level = LogLevel.Information,
        Message = "Supplier return {ReturnId} posted as {Number} ({StockDocumentNumber}); pending credit claim {CreditClaim}")]
    private static partial void ReturnPosted(ILogger logger, Guid returnId, string number, string stockDocumentNumber, decimal creditClaim);
}
