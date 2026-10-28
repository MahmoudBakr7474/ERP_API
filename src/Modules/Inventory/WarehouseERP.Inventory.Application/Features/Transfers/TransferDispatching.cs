using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers;

// Dispatching, directly or by approval, as one transaction: a transit location is opened for the transfer, the dispatch
// document moves every line into it, and the transfer is numbered after its dispatch. The location is saved before the
// ledger runs (stock rows point at it); a failure later rolls back everything, the location included.
public sealed partial class TransferDispatching(
    ITransactionRunner transactions,
    IInventoryLocks locks,
    IIdempotencyStore requests,
    IStockTransferRepository transfers,
    IStockTransferReadStore readStore,
    IStockDocumentRepository documents,
    ITransitLocations transitLocations,
    StockDocumentPosting posting,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<TransferDispatching> logger)
{
    private const string DispatchOperation = "DispatchStockTransfer";
    private const string ApproveOperation = "ApproveStockTransfer";

    public Task<Result<StockTransferPostedResponse>> DispatchAsync(
        Guid transferId, Guid idempotencyKey, byte[] rowVersion, bool byApproval, CancellationToken ct) =>
        transactions.RunAsync(token => DispatchOnceAsync(transferId, idempotencyKey, rowVersion, byApproval, token), ct);

    private async Task<Result<StockTransferPostedResponse>> DispatchOnceAsync(
        Guid transferId, Guid idempotencyKey, byte[] rowVersion, bool byApproval, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var operation = byApproval ? ApproveOperation : DispatchOperation;
        var fingerprint = RequestFingerprint.Of(operation, transferId, rowVersion, userId);

        if (!await locks.TryLockRequestAsync(idempotencyKey, ct))
            return StockErrors.Busy;
        var previous = await requests.FindAsync(idempotencyKey, ct);
        if (previous is not null)
        {
            if (previous.Fingerprint != fingerprint || previous.DocumentId is not { } postedId)
                return IdempotencyErrors.KeyReused;
            return await readStore.FindPostedAsync(transferId, postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at document {postedId}, which is not posted.");
        }

        var transfer = await transfers.GetForUpdateAsync(transferId, expectedRowVersion: null, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;
        if (!transfer.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = transfer.CanDispatch(byApproval);
        if (allowed.IsFailure)
            return allowed.Error;

        if (!await locks.TryLockWarehousesAsync([transfer.SourceWarehouseId, transfer.TransitWarehouseId], exclusive: false, ct))
            return StockErrors.Busy;
        var reference = StockDocumentNumbers.TransferReference(transfer.DraftNumber);
        var transitLocationId = await transitLocations.OpenAsync(transfer.TransitWarehouseId, reference, "ترانزيت " + reference, ct);
        if (transitLocationId is not { } locationId)
            return StockTransferErrors.TransitWarehouseInvalid;
        var now = clock.GetUtcNow().UtcDateTime;
        var dispatch = StockDocument.CreateTransferDispatch(transfer, locationId, userId, now);
        documents.Add(dispatch);
        var opened = await documents.SaveChangesAsync(ct);
        if (opened.IsFailure)
            return opened.Error;

        var posted = await posting.ApplyAsync(dispatch, PostingMode.Generated, byApproval ? userId : null, ct);
        if (posted.IsFailure)
            return posted.Error;
        transfer.MarkDispatched(posted.Value.Number, locationId, byApproval, userId, now);
        requests.Add(IdempotencyRecord.Create(idempotencyKey, operation, fingerprint, transfer.Id, dispatch.Id, userId, now));
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferDispatched(logger, transfer.Id, posted.Value.Number);
        return new StockTransferPostedResponse(
            transfer.Id, dispatch.Id, posted.Value.Number, transfer.Status, posted.Value.PostedAtUtc, posted.Value.OverReservations);
    }

    [LoggerMessage(EventId = 5020, Level = LogLevel.Information, Message = "Transfer {TransferId} dispatched as {Number}")]
    private static partial void TransferDispatched(ILogger logger, Guid transferId, string number);
}
