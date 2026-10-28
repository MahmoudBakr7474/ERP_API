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

// Purchasing's goods receipts and supplier returns become PurchaseReceipt and PurchaseReturn documents, checked and posted
// by the same engine as every other document (StockDocumentPosting.ApplyAsync), inside the caller's transaction. Nothing
// is saved here: the caller's save point writes the stock document together with its own. Purchasing checked the
// warehouse scope and the approval; Inventory checks everything about stock again after taking its locks.
public sealed partial class PurchaseStockPosting(
    IStockDocumentRepository documents, StockReferences references, StockDocumentPosting posting, ICurrentUser currentUser,
    TimeProvider clock, ILogger<PurchaseStockPosting> logger)
    : IPurchaseStockPosting
{
    public Task<PurchaseStockOutcome> PostReceiptAsync(PurchaseStockRequest request, CancellationToken ct) =>
        PostAsync(StockDocumentType.PurchaseReceipt, request, ct);

    public Task<PurchaseStockOutcome> PostReturnAsync(PurchaseStockRequest request, CancellationToken ct) =>
        PostAsync(StockDocumentType.PurchaseReturn, request, ct);

    private async Task<PurchaseStockOutcome> PostAsync(StockDocumentType documentType, PurchaseStockRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
            throw new ArgumentException("A purchase receipt or return moves at least one line.", nameof(request));

        var posted = await PostOrRefuseAsync(documentType, request, ct);
        if (posted.IsFailure)
            return PurchaseStockOutcome.Refused(posted.Error.Code, posted.Error.Message, posted.Error.Type == ErrorType.Conflict);

        PurchaseStockPosted(logger, posted.Value.Id, posted.Value.Number, request.SourceDocumentId);
        return PurchaseStockOutcome.Posted(posted.Value.Id, posted.Value.Number, posted.Value.PostedAtUtc);
    }

    private async Task<Result<PostedDocumentResponse>> PostOrRefuseAsync(
        StockDocumentType documentType, PurchaseStockRequest request, CancellationToken ct)
    {
        if (await documents.HasSourceAsync(request.SourceDocumentId, ct))
            return StockDocumentErrors.SourceAlreadyPosted;
        var warehouse = await references.UsableWarehouseAsync(request.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var lines = await references.ResolveLinesAsync(documentType, request.WarehouseId,
            request.Lines.Select(l => new StockLineRequest(l.ProductId, l.ProductUnitId, l.Quantity, l.LocationId, l.UnitCost)).ToList(), ct);
        if (lines.IsFailure)
            return lines.Error;

        var created = StockDocument.CreatePurchase(
            documentType, request.WarehouseId, warehouse.Value.BranchId, request.SourceDocumentId, request.SourceNumber, request.Notes,
            lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        documents.Add(created.Value);
        return await posting.ApplyAsync(created.Value, PostingMode.Generated, request.ApprovedByUserId, ct);
    }

    [LoggerMessage(EventId = 5040, Level = LogLevel.Information,
        Message = "Stock document {DocumentId} posted as {Number} for purchasing document {SourceDocumentId}")]
    private static partial void PurchaseStockPosted(ILogger logger, Guid documentId, string number, Guid sourceDocumentId);
}
