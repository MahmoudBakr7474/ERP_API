using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reservations;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// Sales deliveries and customer returns become SalesDelivery and SalesReturn documents, checked and posted by the same
// engine as every other document (StockDocumentPosting.ApplyAsync), inside the caller's transaction. A delivery first
// locks the reservations it consumes (reservation rows come before the warehouse lock, as when releasing), takes each line
// out of its order line's reservation, then posts: the ledger lowers on hand and reserved together and still needs the
// stock in the picked location. A return comes back into sellable stock at the cost its delivery line left with. Nothing
// is saved here: the caller's save point writes the stock document and the reservations with its own document.
public sealed partial class SalesStockPosting(
    IStockDocumentRepository documents, IStockReservationRepository reservations, StockReferences references,
    StockDocumentPosting posting, ICurrentUser currentUser, TimeProvider clock, ILogger<SalesStockPosting> logger)
    : ISalesStockPosting
{
    public async Task<SalesStockOutcome> PostDeliveryAsync(SalesDeliveryStockRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A delivery moves at least one line.", nameof(request));

        var posted = await DeliverOrRefuseAsync(request, ct);
        if (posted.IsFailure)
            return Refused(posted.Error);
        SalesStockPosted(logger, posted.Value.DocumentId!.Value, posted.Value.Number!, request.SourceDocumentId);
        return posted.Value;
    }

    public async Task<SalesStockOutcome> PostReturnAsync(SalesReturnStockRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A return moves at least one line.", nameof(request));

        var posted = await ReturnOrRefuseAsync(request, ct);
        if (posted.IsFailure)
            return Refused(posted.Error);
        SalesStockPosted(logger, posted.Value.DocumentId!.Value, posted.Value.Number!, request.SourceDocumentId);
        return posted.Value;
    }

    private async Task<Result<SalesStockOutcome>> DeliverOrRefuseAsync(SalesDeliveryStockRequest request, CancellationToken ct)
    {
        if (await documents.HasSourceAsync(request.SourceDocumentId, ct))
            return StockDocumentErrors.SourceAlreadyPosted;
        var held = (await reservations.GetForUpdateAsync(request.Lines.Select(l => l.ReservationLineId).Distinct().ToList(), ct))
            .ToDictionary(r => r.SourceLineId);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!held.TryGetValue(line.ReservationLineId, out var reservation) || reservation.SourceDocumentId != request.ReservationSourceId
                || reservation.WarehouseId != request.WarehouseId || reservation.ProductId != line.ProductId)
                return ReservationErrors.NotFound(i + 1);
        }

        var created = await CreateAsync(StockDocumentType.SalesDelivery, request.SourceDocumentId, request.SourceNumber,
            request.WarehouseId, request.Notes,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList(),
            returnsLineIds: null, ct);
        if (created.IsFailure)
            return created.Error;
        var document = created.Value;
        var stockLines = document.Lines.OrderBy(l => l.LineNumber).ToList();
        var userId = currentUser.UserId;
        var now = clock.GetUtcNow().UtcDateTime;
        for (var i = 0; i < stockLines.Count; i++)
        {
            var consumed = held[request.Lines[i].ReservationLineId].Consume(stockLines[i].BaseQuantity, document.Id, userId, now, i + 1);
            if (consumed.IsFailure)
                return consumed.Error;
        }
        return await PostAsync(document, stockLines, approvedBy: null, ct);
    }

    private async Task<Result<SalesStockOutcome>> ReturnOrRefuseAsync(SalesReturnStockRequest request, CancellationToken ct)
    {
        if (await documents.HasSourceAsync(request.SourceDocumentId, ct))
            return StockDocumentErrors.SourceAlreadyPosted;
        var delivered = (await documents.FindDeliveredLinesAsync(
                request.Lines.Select(l => l.DeliveredStockLineId).Distinct().ToList(), StockDocumentType.SalesDelivery, ct))
            .ToDictionary(l => l.LineId);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!delivered.TryGetValue(line.DeliveredStockLineId, out var deliveredLine) || deliveredLine.WarehouseId != request.WarehouseId
                || deliveredLine.ProductId != line.ProductId)
                return StockDocumentErrors.DeliveryLineNotFound(i + 1);
        }

        var created = await CreateAsync(StockDocumentType.SalesReturn, request.SourceDocumentId, request.SourceNumber,
            request.WarehouseId, request.Notes,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId)).ToList(),
            request.Lines.Select(l => l.DeliveredStockLineId).ToList(), ct);
        if (created.IsFailure)
            return created.Error;
        return await PostAsync(created.Value, created.Value.Lines.OrderBy(l => l.LineNumber).ToList(), request.ApprovedByUserId, ct);
    }

    private async Task<Result<StockDocument>> CreateAsync(
        StockDocumentType documentType, Guid sourceDocumentId, string sourceNumber, Guid warehouseId, string? notes,
        IReadOnlyList<StockLineRequest> lines, List<Guid>? returnsLineIds, CancellationToken ct)
    {
        var warehouse = await references.UsableWarehouseAsync(warehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var resolved = await references.ResolveLinesAsync(documentType, warehouseId, lines, ct);
        if (resolved.IsFailure)
            return resolved.Error;
        var inputs = returnsLineIds is null
            ? resolved.Value
            : resolved.Value.Select((l, i) => l with { ReturnsLineId = returnsLineIds[i] }).ToList();

        var created = StockDocument.CreateSales(
            documentType, warehouseId, warehouse.Value.BranchId, sourceDocumentId, sourceNumber, notes, inputs,
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        documents.Add(created.Value);
        return created.Value;
    }

    // Posts the document and reports each line's cost, positive, in the order of the request.
    private async Task<Result<SalesStockOutcome>> PostAsync(
        StockDocument document, IReadOnlyList<StockDocumentLine> lines, Guid? approvedBy, CancellationToken ct)
    {
        var values = new Dictionary<Guid, decimal>();
        var posted = await posting.ApplyAsync(document, PostingMode.Generated, approvedBy, values, ct);
        if (posted.IsFailure)
            return posted.Error;
        return SalesStockOutcome.Posted(posted.Value.Id, posted.Value.Number, posted.Value.PostedAtUtc,
            lines.Select(l => new SalesStockLine(l.Id, l.BaseQuantity, Math.Abs(values[l.Id]))).ToList());
    }

    private static SalesStockOutcome Refused(Error error) =>
        SalesStockOutcome.Refused(error.Code, error.Message, error.Type == ErrorType.Conflict);

    [LoggerMessage(EventId = 5052, Level = LogLevel.Information,
        Message = "Stock document {DocumentId} posted as {Number} for sales document {SourceDocumentId}")]
    private static partial void SalesStockPosted(ILogger logger, Guid documentId, string number, Guid sourceDocumentId);
}
