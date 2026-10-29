using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// POS checkouts and POS customer returns become PosSale and PosReturn documents, checked and posted by the same engine as
// every other document (StockDocumentPosting.ApplyAsync), inside the caller's transaction. A sale is an ordinary unreserved
// issue: the ledger takes only stock nobody reserved, under the warehouse lock, and still needs it in the selling location,
// so a till never sells what a sales order holds and two tills never sell the last unit twice. A count's freeze stops it
// like any posting. A return comes back into sellable stock at the cost its sale line left with. Nothing is saved here: the
// caller's save point writes the stock document with its own.
public sealed partial class PosStockPosting(
    IStockDocumentRepository documents, StockReferences references, StockDocumentPosting posting, ICurrentUser currentUser,
    TimeProvider clock, ILogger<PosStockPosting> logger)
    : IPosStockPosting
{
    public async Task<PosStockOutcome> PostSaleAsync(PosSaleStockRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A POS sale moves at least one line.", nameof(request));

        var posted = await SellOrRefuseAsync(request, ct);
        if (posted.IsFailure)
            return Refused(posted.Error);
        PosStockPosted(logger, posted.Value.DocumentId!.Value, posted.Value.Number!, request.SourceDocumentId);
        return posted.Value;
    }

    public async Task<PosStockOutcome> PostReturnAsync(PosReturnStockRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A POS return moves at least one line.", nameof(request));

        var posted = await ReturnOrRefuseAsync(request, ct);
        if (posted.IsFailure)
            return Refused(posted.Error);
        PosStockPosted(logger, posted.Value.DocumentId!.Value, posted.Value.Number!, request.SourceDocumentId);
        return posted.Value;
    }

    private async Task<Result<PosStockOutcome>> SellOrRefuseAsync(PosSaleStockRequest request, CancellationToken ct)
    {
        if (await documents.HasSourceAsync(request.SourceDocumentId, ct))
            return StockDocumentErrors.SourceAlreadyPosted;
        var created = await CreateAsync(StockDocumentType.PosSale, request.SourceDocumentId, request.SourceNumber,
            request.WarehouseId, request.Notes,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList(),
            returnsLineIds: null, ct);
        if (created.IsFailure)
            return created.Error;
        return await PostAsync(created.Value, ct);
    }

    private async Task<Result<PosStockOutcome>> ReturnOrRefuseAsync(PosReturnStockRequest request, CancellationToken ct)
    {
        if (await documents.HasSourceAsync(request.SourceDocumentId, ct))
            return StockDocumentErrors.SourceAlreadyPosted;
        var sold = (await documents.FindDeliveredLinesAsync(
                request.Lines.Select(l => l.SoldStockLineId).Distinct().ToList(), StockDocumentType.PosSale, ct))
            .ToDictionary(l => l.LineId);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!sold.TryGetValue(line.SoldStockLineId, out var soldLine) || soldLine.WarehouseId != request.WarehouseId
                || soldLine.ProductId != line.ProductId)
                return StockDocumentErrors.SoldLineNotFound(i + 1);
        }

        var created = await CreateAsync(StockDocumentType.PosReturn, request.SourceDocumentId, request.SourceNumber,
            request.WarehouseId, request.Notes,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList(),
            request.Lines.Select(l => l.SoldStockLineId).ToList(), ct);
        if (created.IsFailure)
            return created.Error;
        return await PostAsync(created.Value, ct);
    }

    private async Task<Result<StockDocument>> CreateAsync(
        StockDocumentType documentType, Guid sourceDocumentId, string sourceNumber, Guid warehouseId, string? notes,
        IReadOnlyList<StockLineRequest> lines, List<Guid>? returnsLineIds, CancellationToken ct)
    {
        var warehouse = await references.UsableWarehouseAsync(warehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        if (documentType == StockDocumentType.PosSale && !warehouse.Value.AllowsSelling)
            return StockDocumentErrors.WarehouseNotSelling;
        var resolved = await references.ResolveLinesAsync(documentType, warehouseId, lines, ct);
        if (resolved.IsFailure)
            return resolved.Error;
        var inputs = returnsLineIds is null
            ? resolved.Value
            : resolved.Value.Select((l, i) => l with { ReturnsLineId = returnsLineIds[i] }).ToList();

        var created = StockDocument.CreatePos(
            documentType, warehouseId, warehouse.Value.BranchId, sourceDocumentId, sourceNumber, notes, inputs,
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        documents.Add(created.Value);
        return created.Value;
    }

    // Posts the document and reports each line's cost, positive, in the order of the request.
    private async Task<Result<PosStockOutcome>> PostAsync(StockDocument document, CancellationToken ct)
    {
        var lines = document.Lines.OrderBy(l => l.LineNumber).ToList();
        var values = new Dictionary<Guid, decimal>();
        var posted = await posting.ApplyAsync(document, PostingMode.Generated, approvedBy: null, values, ct);
        if (posted.IsFailure)
            return posted.Error;
        return PosStockOutcome.Posted(posted.Value.Id, posted.Value.Number, posted.Value.PostedAtUtc,
            lines.Select(l => new PosStockLine(l.Id, l.BaseQuantity, Math.Abs(values[l.Id]))).ToList());
    }

    private static PosStockOutcome Refused(Error error) =>
        PosStockOutcome.Refused(error.Code, error.Message, error.Type == ErrorType.Conflict);

    [LoggerMessage(EventId = 5060, Level = LogLevel.Information,
        Message = "Stock document {DocumentId} posted as {Number} for POS document {SourceDocumentId}")]
    private static partial void PosStockPosted(ILogger logger, Guid documentId, string number, Guid sourceDocumentId);
}
