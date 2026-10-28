using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Shared;

// Posting as one transaction: the document, balances, availability, movements, valuation, its number and its idempotency
// record commit together or not at all. Locks are always taken in the same order (request key, the document, transfer or
// count row, warehouses in id order, availability rows, balance rows, valuation rows, number series), which keeps
// concurrent postings from deadlocking; a deadlock that still happens runs the whole posting again, and so does a commit
// whose outcome is unknown: the first thing a run does is look for its own idempotency record. Transfers and counts post
// the documents they produce through ApplyAsync, inside their own transaction.
public sealed partial class StockDocumentPosting(
    ITransactionRunner transactions,
    IInventoryLocks locks,
    IIdempotencyStore requests,
    IStockDocumentRepository documents,
    IStockDocumentReadStore postedDocuments,
    IStockTransferRepository transfers,
    ITransitLocations transitLocations,
    StockReferences references,
    IStockLedger ledger,
    IInventoryValuation valuation,
    IStockFreezes freezes,
    IDocumentNumbers numbers,
    IBranchDirectory branches,
    ICompanyDirectory companies,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<StockDocumentPosting> logger)
{
    private const string PostOperation = "PostStockDocument";
    private const string ApproveOperation = "ApproveStockDocument";

    public Task<Result<PostedDocumentResponse>> PostAsync(
        Guid documentId, Guid idempotencyKey, byte[] rowVersion, bool byApproval, CancellationToken ct) =>
        transactions.RunAsync(token => PostOnceAsync(documentId, idempotencyKey, rowVersion, byApproval, token), ct);

    /// <summary>
    /// Posts a document inside the caller's running transaction: takes its warehouses' locks in order, checks what it
    /// refers to, the opening-stock rule and the count freezes, applies it to the ledger, values it, numbers it and marks
    /// it posted. Posting a transfer's loss adjustment also closes the transfer. The caller saves; on failure its
    /// transaction rolls everything back.
    /// </summary>
    public async Task<Result<PostedDocumentResponse>> ApplyAsync(
        StockDocument document, PostingMode mode, Guid? approvedBy, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var places = StockReferences.PlacesOf(document);
        var warehouseIds = places.Select(p => p.WarehouseId).Append(document.WarehouseId).Distinct().ToList();
        // Opening stock takes its warehouse alone, so nothing else posts there while it checks that it still may.
        var exclusive = document.DocumentType == StockDocumentType.OpeningBalance;
        if (!await locks.TryLockWarehousesAsync(warehouseIds, exclusive, ct))
            return StockErrors.Busy;
        var checkedReferences = await references.CheckForPostingAsync(document, ct);
        if (checkedReferences.IsFailure)
            return checkedReferences.Error;
        var refs = checkedReferences.Value;
        if (document.DocumentType == StockDocumentType.OpeningBalance
            && await documents.HasPostedBeyondOpeningAsync(document.WarehouseId, ct))
            return StockDocumentErrors.OpeningClosed;
        // A count's freeze is persisted, so it holds no lock: every posting looks for it once its warehouse locks are held.
        var frozen = await freezes.FindFrozenAsync(places, document.CountId, ct);
        if (frozen is not null)
            return StockCountErrors.ScopeFrozen(frozen.CountReference);

        var now = clock.GetUtcNow().UtcDateTime;
        var mayOverReserve = document.DocumentType is StockDocumentType.Adjustment or StockDocumentType.Reversal
            or StockDocumentType.CountAdjustment;
        var outcome = await ledger.ApplyAsync(new StockPosting(document.Id, now, mayOverReserve, Changes(document, refs.Warehouses)), ct);
        if (outcome.Shortage is { } shortage)
        {
            var sku = refs.Products[shortage.ProductId].Sku;
            return shortage.LocationId is { } locationId
                ? StockErrors.InsufficientAtLocation(sku, refs.Locations[locationId].Code)
                : StockErrors.Insufficient(sku);
        }

        var valued = await ValueAsync(document, now, ct);
        if (valued.IsFailure)
            return valued.Error;

        var numbered = await NumberAsync(document, now, ct);
        if (numbered.IsFailure)
            return numbered.Error;
        var (number, fiscalYear, sequence) = numbered.Value;
        switch (mode)
        {
            case PostingMode.Direct:
                document.MarkPosted(number, fiscalYear, sequence, byApproval: false, userId, now);
                break;
            case PostingMode.Approval:
                document.MarkPosted(number, fiscalYear, sequence, byApproval: true, userId, now);
                break;
            default:
                document.MarkPostedGenerated(number, fiscalYear, sequence, approvedBy, userId, now);
                break;
        }
        if (document.TransferId is { } transferId && document.DocumentType == StockDocumentType.Adjustment)
            await CloseTransferAsync(transferId, number, userId, now, ct);

        DocumentPosted(logger, document.Id, number);
        return new PostedDocumentResponse(document.Id, number, now, outcome.OverReserved
            .Select(o => new OverReservationResponse(
                o.ProductId, refs.Products[o.ProductId].Sku, o.WarehouseId, refs.Warehouses[o.WarehouseId].Code, document.Id,
                number, o.OnHandSellable, o.ReservedQuantity, now))
            .ToList());
    }

    private async Task<Result<PostedDocumentResponse>> PostOnceAsync(
        Guid documentId, Guid idempotencyKey, byte[] rowVersion, bool byApproval, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var operation = byApproval ? ApproveOperation : PostOperation;
        var fingerprint = RequestFingerprint.Of(operation, documentId, rowVersion, userId);

        if (!await locks.TryLockRequestAsync(idempotencyKey, ct))
            return StockErrors.Busy;
        var previous = await requests.FindAsync(idempotencyKey, ct);
        if (previous is not null)
            return previous.Fingerprint == fingerprint ? await ReplayAsync(previous.SubjectId, ct) : IdempotencyErrors.KeyReused;

        var document = await documents.GetForUpdateAsync(documentId, expectedRowVersion: null, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;
        if (!document.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = document.CanPost(byApproval);
        if (allowed.IsFailure)
            return allowed.Error;

        var posted = await ApplyAsync(document, byApproval ? PostingMode.Approval : PostingMode.Direct, approvedBy: null, ct);
        if (posted.IsFailure)
            return posted.Error;
        requests.Add(IdempotencyRecord.Create(idempotencyKey, operation, fingerprint, document.Id, document.Id, userId, posted.Value.PostedAtUtc));

        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        return posted.Value;
    }

    private async Task<Result<PostedDocumentResponse>> ReplayAsync(Guid documentId, CancellationToken ct) =>
        await postedDocuments.FindPostedAsync(documentId, ct)
        ?? throw new InvalidOperationException($"Idempotency record points at document {documentId}, which is not posted.");

    // The approved loss adjustment wrote off what was outstanding: the transfer closes and its transit location retires.
    private async Task CloseTransferAsync(Guid transferId, string number, Guid? userId, DateTime now, CancellationToken ct)
    {
        var transfer = await transfers.GetForUpdateAsync(transferId, expectedRowVersion: null, ct)
            ?? throw new InvalidOperationException($"Loss adjustment points at transfer {transferId}, which does not exist.");
        transfer.CompleteClosing(number, userId, now);
        if (transfer.TransitLocationId is { } locationId)
            await transitLocations.RetireAsync(transfer.TransitWarehouseId, locationId, ct);
    }

    // Each allocation side becomes one signed change in one location; only a physical warehouse's stock is sellable.
    private static List<StockChange> Changes(StockDocument document, IReadOnlyDictionary<Guid, WarehouseInfo> warehouses)
    {
        var changes = new List<StockChange>();
        foreach (var line in document.Lines)
            foreach (var allocation in line.Allocations)
            {
                if (allocation.FromLocationId is { } from)
                {
                    var warehouseId = allocation.FromWarehouseId!.Value;
                    changes.Add(new StockChange(line.Id, allocation.Id, line.ProductId, warehouseId, from, -allocation.BaseQuantity,
                        warehouses[warehouseId].Type == WarehouseType.Physical));
                }
                if (allocation.ToLocationId is { } to)
                {
                    var warehouseId = allocation.ToWarehouseId!.Value;
                    changes.Add(new StockChange(line.Id, allocation.Id, line.ProductId, warehouseId, to, allocation.BaseQuantity,
                        warehouses[warehouseId].Type == WarehouseType.Physical));
                }
            }
        return changes;
    }

    // Moving weighted average, line by line. Stock coming in with a cost is valued at it; an increase without one at the
    // current average (refused for a product that was never valued); a reversed issue comes back at the value it left
    // with; stock going out leaves at the current average. A transfer moves stock inside the company: no valuation.
    private async Task<Result> ValueAsync(StockDocument document, DateTime now, CancellationToken ct)
    {
        if (document.DocumentType is StockDocumentType.TransferDispatch or StockDocumentType.TransferReceipt)
            return Result.Success();

        var productIds = document.Lines.Select(l => l.ProductId).Distinct().ToList();
        var states = new Dictionary<Guid, InventoryCostState>(await valuation.LockAsync(productIds, ct));
        var valued = states.Keys.ToHashSet();
        IReadOnlyDictionary<Guid, decimal> leftWith = document.ReversalOfDocumentId is { } originalId
            ? await valuation.GetLineValuesAsync(originalId, ct)
            : new Dictionary<Guid, decimal>();

        foreach (var line in document.Lines.OrderBy(l => l.LineNumber))
        {
            if (!states.TryGetValue(line.ProductId, out var state))
            {
                state = InventoryCostState.Start(line.ProductId, now);
                states.Add(line.ProductId, state);
                valuation.Add(state);
            }

            CostChange change;
            if (line.Direction == StockDirection.Out)
                change = state.Issue(line.BaseQuantity, now);
            else if (line.UnitCost is { } unitCost)
                change = state.Receive(line.BaseQuantity, line.Quantity * unitCost, now);
            else if (line.ReversesLineId is { } reversedLine && leftWith.TryGetValue(reversedLine, out var value))
                change = state.Receive(line.BaseQuantity, -value, now);
            else if (valued.Contains(line.ProductId))
                change = state.ReceiveAtAverage(line.BaseQuantity, now);
            else
                return ValuationErrors.UnitCostRequired(line.LineNumber);

            valued.Add(line.ProductId);
            valuation.Add(InventoryCostEntry.Record(state, change, document.Id, line.Id, now));
        }
        return Result.Success();
    }

    // The fiscal year is the calendar year in the company's time zone.
    private async Task<Result<(string Number, short FiscalYear, int Sequence)>> NumberAsync(StockDocument document, DateTime now, CancellationToken ct)
    {
        var company = await companies.GetAsync(ct);
        if (company is null)
            return StockDocumentErrors.CompanySetupRequired;
        var branch = (await branches.FindManyAsync([document.BranchId], ct)).Single();
        var fiscalYear = (short)TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId)).Year;
        var sequence = await numbers.NextAsync(document.DocumentType, document.BranchId, fiscalYear, ct);
        return (StockDocumentNumbers.Format(document.DocumentType, branch.Code, fiscalYear, sequence), fiscalYear, sequence);
    }

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Stock document {DocumentId} posted as {Number}")]
    private static partial void DocumentPosted(ILogger logger, Guid documentId, string number);
}
