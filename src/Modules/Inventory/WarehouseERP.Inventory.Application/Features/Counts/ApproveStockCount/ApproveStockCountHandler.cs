using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Counts;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.Counts.ApproveStockCount;

// An approver with the warehouse in scope (its creator included) approves the count as one transaction: under the
// warehouse lock the frozen stock must still match the snapshot, the differences post as a count adjustment (a surplus at
// its line's cost or the average, a shortage at the average, never below zero), and the freeze is released.
public sealed partial class ApproveStockCountHandler(
    ITransactionRunner transactions,
    IInventoryLocks locks,
    IIdempotencyStore requests,
    IStockCountRepository counts,
    IStockCountReadStore readStore,
    IStockDocumentRepository documents,
    StockReferences references,
    StockDocumentPosting posting,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<ApproveStockCountHandler> logger)
    : ICommandHandler<ApproveStockCountCommand, StockCountApprovalResponse>
{
    private const string Operation = "ApproveStockCount";

    public async Task<Result<StockCountApprovalResponse>> HandleAsync(ApproveStockCountCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await CountAccess.AuthorizeAsync(counts, currentUser, command.CountId, ct);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => ApproveAsync(command, expected, token), ct);
    }

    private async Task<Result<StockCountApprovalResponse>> ApproveAsync(ApproveStockCountCommand command, byte[] rowVersion, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var fingerprint = RequestFingerprint.Of(Operation, command.CountId, rowVersion, userId);
        if (!await locks.TryLockRequestAsync(command.IdempotencyKey, ct))
            return StockErrors.Busy;
        var previous = await requests.FindAsync(command.IdempotencyKey, ct);
        if (previous is not null)
        {
            if (previous.Fingerprint != fingerprint)
                return IdempotencyErrors.KeyReused;
            return await readStore.FindApprovedAsync(command.CountId, ct)
                ?? throw new InvalidOperationException($"Idempotency record points at count {command.CountId}, which is not completed.");
        }

        var count = await counts.GetForUpdateAsync(command.CountId, expectedRowVersion: null, ct);
        if (count is null)
            return StockCountErrors.NotFound;
        if (!count.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = count.CanApprove();
        if (allowed.IsFailure)
            return allowed.Error;

        if (!await locks.TryLockWarehousesAsync([count.WarehouseId], exclusive: false, ct))
            return StockErrors.Busy;
        if (!await StockStillMatchesAsync(count, ct))
            return StockCountErrors.StockChangedDuringCount;

        var now = clock.GetUtcNow().UtcDateTime;
        var differences = count.Differences.ToList();
        StockDocument? adjustment = null;
        PostedDocumentResponse? posted = null;
        if (differences.Count > 0)
        {
            var baseUnits = await references.BaseUnitsAsync(differences.Select(d => d.ProductId).Distinct().ToList(), ct);
            var lines = differences
                .Select(d => new GeneratedLineInput(
                    d.ProductId, baseUnits[d.ProductId].Id, baseUnits[d.ProductId].UnitId, Math.Abs(d.Variance),
                    d.Variance > 0 ? StockDirection.In : StockDirection.Out, d.Variance > 0 ? d.UnitCost : null,
                    new StockPlace(count.WarehouseId, d.LocationId)))
                .ToList();
            adjustment = StockDocument.CreateCountAdjustment(count, lines, userId, now);
            documents.Add(adjustment);
            var applied = await posting.ApplyAsync(adjustment, PostingMode.Generated, approvedBy: userId, ct);
            if (applied.IsFailure)
                return applied.Error;
            posted = applied.Value;
        }

        count.Complete(posted?.Number, userId, now);
        requests.Add(IdempotencyRecord.Create(command.IdempotencyKey, Operation, fingerprint, count.Id, adjustment?.Id, userId, now));
        var saved = await counts.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        CountApproved(logger, count.Id, differences.Count);
        return new StockCountApprovalResponse(
            count.Id, adjustment?.Id, posted?.Number, now, posted?.OverReservations ?? Array.Empty<OverReservationResponse>());
    }

    // Postings could not touch the frozen scope, so this only fails if something bypassed the freeze.
    private async Task<bool> StockStillMatchesAsync(StockCount count, CancellationToken ct)
    {
        var current = (await counts.ReadScopeBalancesAsync(count, ct)).ToDictionary(b => (b.ProductId, b.LocationId), b => b.Quantity);
        var expected = count.Lines.Where(l => l.ExpectedQuantity != 0).ToDictionary(l => (l.ProductId, l.LocationId), l => l.ExpectedQuantity);
        return current.Count == expected.Count && current.All(c => expected.TryGetValue(c.Key, out var quantity) && quantity == c.Value);
    }

    [LoggerMessage(EventId = 5036, Level = LogLevel.Information, Message = "Stock count {CountId} approved with {Differences} differences")]
    private static partial void CountApproved(ILogger logger, Guid countId, int differences);
}
