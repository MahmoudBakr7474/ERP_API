using System.Globalization;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Transfers.ReceiveStockTransfer;

// Someone who receives transfers into the destination warehouse posts what arrived, part or all, as one transaction: a
// receipt document moves it from the transit location into destination locations and the transfer records it. Never more
// than is still in transit; once nothing is, the transfer is received and its transit location retired.
public sealed partial class ReceiveStockTransferHandler(
    ITransactionRunner transactions,
    IInventoryLocks locks,
    IIdempotencyStore requests,
    IStockTransferRepository transfers,
    IStockTransferReadStore readStore,
    IStockDocumentRepository documents,
    ITransitLocations transitLocations,
    StockReferences references,
    StockDocumentPosting posting,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<ReceiveStockTransferHandler> logger)
    : ICommandHandler<ReceiveStockTransferCommand, StockTransferPostedResponse>
{
    private const string Operation = "ReceiveStockTransfer";

    public async Task<Result<StockTransferPostedResponse>> HandleAsync(ReceiveStockTransferCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await TransferAccess.AuthorizeAsync(transfers, currentUser, command.TransferId, TransferAccess.Side.Destination, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ReceiveAsync(command, expected, token), ct);
    }

    private async Task<Result<StockTransferPostedResponse>> ReceiveAsync(
        ReceiveStockTransferCommand command, byte[] rowVersion, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var content = string.Join(';', command.Lines.Select(l =>
            string.Create(CultureInfo.InvariantCulture, $"{l.LineId:N}:{l.Quantity}:{l.LocationId:N}")));
        var fingerprint = RequestFingerprint.Of(Operation, command.TransferId, rowVersion, userId, content);

        if (!await locks.TryLockRequestAsync(command.IdempotencyKey, ct))
            return StockErrors.Busy;
        var previous = await requests.FindAsync(command.IdempotencyKey, ct);
        if (previous is not null)
        {
            if (previous.Fingerprint != fingerprint || previous.DocumentId is not { } postedId)
                return IdempotencyErrors.KeyReused;
            return await readStore.FindPostedAsync(command.TransferId, postedId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at document {postedId}, which is not posted.");
        }

        var transfer = await transfers.GetForUpdateAsync(command.TransferId, expectedRowVersion: null, ct);
        if (transfer is null)
            return StockTransferErrors.NotFound;
        if (!transfer.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;

        var productOfLine = transfer.Lines.ToDictionary(l => l.Id, l => l.ProductId);
        var baseUnits = await references.BaseUnitsAsync(productOfLine.Values.Distinct().ToList(), ct);
        var defaultLocation = await references.DefaultLocationAsync(transfer.DestinationWarehouseId, ct);
        var receipts = new List<TransferReceiptInput>(command.Lines.Count);
        foreach (var line in command.Lines)
        {
            if (!productOfLine.TryGetValue(line.LineId, out var productId))
                return StockTransferDomainErrors.UnknownLine(line.LineId);
            var baseUnit = baseUnits[productId];
            receipts.Add(new TransferReceiptInput(
                line.LineId, line.Quantity, line.LocationId ?? defaultLocation?.Id ?? Guid.Empty, baseUnit.Id, baseUnit.UnitId,
                baseUnit.UnitAllowsFraction));
        }
        var allowed = transfer.CheckReceipt(receipts);
        if (allowed.IsFailure)
            return allowed.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var receipt = StockDocument.CreateTransferReceipt(transfer, receipts, userId, now);
        documents.Add(receipt);
        var posted = await posting.ApplyAsync(receipt, PostingMode.Generated, approvedBy: null, ct);
        if (posted.IsFailure)
            return posted.Error;
        transfer.RecordReceipt(receipts, posted.Value.Number, userId, now);
        if (transfer.Status == StockTransferStatus.Received && transfer.TransitLocationId is { } transitLocationId)
            await transitLocations.RetireAsync(transfer.TransitWarehouseId, transitLocationId, ct);
        requests.Add(IdempotencyRecord.Create(command.IdempotencyKey, Operation, fingerprint, transfer.Id, receipt.Id, userId, now));
        var saved = await transfers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        TransferReceived(logger, transfer.Id, posted.Value.Number);
        return new StockTransferPostedResponse(
            transfer.Id, receipt.Id, posted.Value.Number, transfer.Status, posted.Value.PostedAtUtc, posted.Value.OverReservations);
    }

    [LoggerMessage(EventId = 5026, Level = LogLevel.Information, Message = "Transfer {TransferId} received {Number}")]
    private static partial void TransferReceived(ILogger logger, Guid transferId, string number);
}
