using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Deliveries;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Sales.Application.Contracts.Returns;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Returns.ApproveSalesReturn;

// An approver (endpoint permission, warehouse scope), its creator included (CLAUDE.md), approves in one transaction: the
// request's key is locked and looked up (a retry gets the first answer), the return is locked at the version the approver
// saw, then its order and its delivery (concurrent returns of one delivery wait for each other, so cumulative returns can't
// exceed what was delivered), the order splits each line between goods never invoiced and invoiced goods (a pending
// customer credit), the return is numbered, and Inventory takes the goods back into sellable stock at the cost their
// delivery lines left with. Everything commits together or not at all. No credit note, refund or payment is made.
public sealed partial class ApproveSalesReturnHandler(
    ITransactionRunner transactions, IdempotentRequests requests, ISalesReturnRepository returns, ISalesOrderRepository orders,
    ISalesDeliveryRepository deliveries, ISalesReturnReadStore readStore, DocumentNumbering numbering, ISalesStockPosting stock,
    ICurrentUser currentUser, TimeProvider clock, ILogger<ApproveSalesReturnHandler> logger)
    : ICommandHandler<ApproveSalesReturnCommand, SalesReturnApprovedResponse>
{
    private const string Operation = "ApproveSalesReturn";

    public async Task<Result<SalesReturnApprovedResponse>> HandleAsync(ApproveSalesReturnCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await ReturnAccess.AuthorizeAsync(returns, currentUser, command.ReturnId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command, expected, token), ct);
    }

    private async Task<Result<SalesReturnApprovedResponse>> ApproveAsync(ApproveSalesReturnCommand command, byte[] expected, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.ReturnId, expected, userId);
        var previous = await requests.BeginAsync(command.IdempotencyKey, fingerprint, ct);
        if (previous.IsFailure)
            return previous.Error;
        if (previous.Value is { } approvedId)
            return await readStore.FindApprovedAsync(approvedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at return {approvedId}, which is not approved.");

        var salesReturn = await returns.GetForUpdateAsync(command.ReturnId, expected, ct);
        if (salesReturn is null)
            return ReturnErrors.NotFound;
        var allowed = salesReturn.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;
        var order = await orders.GetForUpdateAsync(salesReturn.OrderId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Return {salesReturn.Id} points at order {salesReturn.OrderId}, which does not exist.");
        var delivery = await deliveries.GetForUpdateAsync(salesReturn.DeliveryId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Return {salesReturn.Id} points at delivery {salesReturn.DeliveryId}, which does not exist.");
        var now = clock.GetUtcNow().UtcDateTime;
        var returnedFromDelivery = delivery.RecordReturn(salesReturn.DeliveryQuantities());
        if (returnedFromDelivery.IsFailure)
            return returnedFromDelivery.Error;
        var number = await numbering.NextAsync(SalesDocumentType.Return, salesReturn.BranchId, ct);
        if (number.IsFailure)
            return number.Error;
        var splits = order.RecordReturn(salesReturn.OrderQuantities(), number.Value.Number, userId, now);
        if (splits.IsFailure)
            return splits.Error;

        var lines = salesReturn.Lines.OrderBy(l => l.LineNumber).ToList();
        var posted = await stock.PostReturnAsync(new SalesReturnStockRequest(
            salesReturn.Id, number.Value.Number, salesReturn.WarehouseId, salesReturn.Notes, userId,
            lines.Select(l => new SalesReturnStockLine(
                delivery.FindLine(l.DeliveryLineId)!.StockLineId!.Value, l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList()), ct);
        if (!posted.Succeeded)
            return StockErrors.Refused(posted);

        salesReturn.Approve(number.Value, posted.DocumentId!.Value, posted.Number!,
            posted.Lines.Select(l => (l.StockLineId, l.CostAmount)).ToList(), splits.Value, userId, now);
        requests.Complete(command.IdempotencyKey, Operation, fingerprint, salesReturn.Id, userId, now);

        var saved = await returns.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ReturnApproved(logger, salesReturn.Id, number.Value.Number, posted.Number!);
        return new SalesReturnApprovedResponse(salesReturn.Id, number.Value.Number, posted.DocumentId.Value, posted.Number!,
            salesReturn.CostAmount, salesReturn.CreditTotalAmount, now);
    }

    [LoggerMessage(EventId = 7063, Level = LogLevel.Information,
        Message = "Customer return {ReturnId} approved as {Number}, back in stock as {StockDocumentNumber}")]
    private static partial void ReturnApproved(ILogger logger, Guid returnId, string number, string stockDocumentNumber);
}
