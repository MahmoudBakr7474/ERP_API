using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Stock;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// Posting, directly or by approval, as one transaction: the document, balances, availability, movements, valuation,
// its number and its idempotency record commit together or not at all. Locks are always taken in the same order
// (request key, document row, warehouses, availability rows, balance rows, valuation rows, number series), which keeps
// concurrent postings from deadlocking; a deadlock that still happens runs the whole posting again, and so does a
// commit whose outcome is unknown: the first thing a run does is look for its own idempotency record.
public sealed partial class StockDocumentPosting(
    ITransactionRunner transactions,
    IInventoryLocks locks,
    IIdempotencyStore requests,
    IStockDocumentRepository documents,
    IStockDocumentReadStore postedDocuments,
    StockReferences references,
    IStockLedger ledger,
    IInventoryValuation valuation,
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

    private async Task<Result<PostedDocumentResponse>> PostOnceAsync(
        Guid documentId, Guid idempotencyKey, byte[] rowVersion, bool byApproval, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var operation = byApproval ? ApproveOperation : PostOperation;
        var fingerprint = Fingerprint(operation, documentId, rowVersion, userId);

        if (!await locks.TryLockRequestAsync(idempotencyKey, ct))
            return StockErrors.Busy;
        var previous = await requests.FindAsync(idempotencyKey, ct);
        if (previous is not null)
            return previous.Fingerprint == fingerprint ? await ReplayAsync(previous.DocumentId, ct) : IdempotencyErrors.KeyReused;

        var document = await documents.GetForUpdateAsync(documentId, expectedRowVersion: null, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;
        if (!document.RowVersion.AsSpan().SequenceEqual(rowVersion))
            return CommonErrors.ConcurrencyConflict;
        var allowed = document.CanPost(byApproval, userId);
        if (allowed.IsFailure)
            return allowed.Error;

        // Opening stock takes its warehouse alone, so nothing else posts there while it checks that it still may.
        var exclusive = document.DocumentType == StockDocumentType.OpeningBalance;
        if (!await locks.TryLockWarehousesAsync([document.WarehouseId], exclusive, ct))
            return StockErrors.Busy;
        var checkedReferences = await references.CheckForPostingAsync(document, ct);
        if (checkedReferences.IsFailure)
            return checkedReferences.Error;
        var refs = checkedReferences.Value;
        if (document.DocumentType == StockDocumentType.OpeningBalance
            && await documents.HasPostedBeyondOpeningAsync(document.WarehouseId, ct))
            return StockDocumentErrors.OpeningClosed;

        var now = clock.GetUtcNow().UtcDateTime;
        var mayOverReserve = document.DocumentType is StockDocumentType.Adjustment or StockDocumentType.Reversal;
        var outcome = await ledger.ApplyAsync(new StockPosting(document.Id, now, mayOverReserve, Changes(document, refs.Warehouse)), ct);
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
        document.MarkPosted(number, fiscalYear, sequence, byApproval, userId, now);
        requests.Add(IdempotencyRecord.Create(idempotencyKey, operation, fingerprint, document.Id, userId, now));

        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentPosted(logger, document.Id, number);
        return new PostedDocumentResponse(document.Id, number, now, outcome.OverReserved
            .Select(o => new OverReservationResponse(
                o.ProductId, refs.Products[o.ProductId].Sku, o.WarehouseId, refs.Warehouse.Code, document.Id, number,
                o.OnHandSellable, o.ReservedQuantity, now))
            .ToList());
    }

    private async Task<Result<PostedDocumentResponse>> ReplayAsync(Guid documentId, CancellationToken ct) =>
        await postedDocuments.FindPostedAsync(documentId, ct)
        ?? throw new InvalidOperationException($"Idempotency record points at document {documentId}, which is not posted.");

    // Each allocation side becomes one signed change in one location.
    private static List<StockChange> Changes(StockDocument document, WarehouseInfo warehouse)
    {
        var sellable = warehouse.Type == WarehouseType.Physical;
        var changes = new List<StockChange>();
        foreach (var line in document.Lines)
            foreach (var allocation in line.Allocations)
            {
                if (allocation.FromLocationId is { } from)
                    changes.Add(new StockChange(line.Id, allocation.Id, line.ProductId, allocation.FromWarehouseId!.Value, from, -allocation.BaseQuantity, sellable));
                if (allocation.ToLocationId is { } to)
                    changes.Add(new StockChange(line.Id, allocation.Id, line.ProductId, allocation.ToWarehouseId!.Value, to, allocation.BaseQuantity, sellable));
            }
        return changes;
    }

    // Moving weighted average, line by line. Stock coming in with a cost is valued at it; an increase without one at the
    // current average (refused for a product that was never valued); a reversed issue comes back at the value it left
    // with; stock going out leaves at the current average.
    private async Task<Result> ValueAsync(StockDocument document, DateTime now, CancellationToken ct)
    {
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

    // What makes two requests the same: the action, the document, the version the client saw and who sent it.
    private static string Fingerprint(string operation, Guid documentId, byte[] rowVersion, Guid? userId)
    {
        var request = string.Create(CultureInfo.InvariantCulture, $"{operation}|{documentId:N}|{Convert.ToBase64String(rowVersion)}|{userId:N}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));
    }

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Stock document {DocumentId} posted as {Number}")]
    private static partial void DocumentPosted(ILogger logger, Guid documentId, string number);
}
