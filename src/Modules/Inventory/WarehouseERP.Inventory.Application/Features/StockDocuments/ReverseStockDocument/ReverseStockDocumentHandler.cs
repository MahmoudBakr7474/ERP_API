using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ReverseStockDocument;

// Someone who manages the posted document's type creates its reversal, submitted for approval at once; another user
// approves it. One reversal per document (the database guards it when two requests race).
public sealed partial class ReverseStockDocumentHandler(
    IStockDocumentRepository documents, ICurrentUser currentUser, TimeProvider clock, ILogger<ReverseStockDocumentHandler> logger)
    : ICommandHandler<ReverseStockDocumentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(ReverseStockDocumentCommand command, CancellationToken ct)
    {
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: true, ct);
        if (access.IsFailure)
            return access.Error;
        var original = await documents.GetForUpdateAsync(command.DocumentId, expectedRowVersion: null, ct);
        if (original is null)
            return StockDocumentErrors.NotFound;
        if (await documents.HasReversalAsync(original.Id, ct))
            return StockDocumentErrors.AlreadyReversed;

        var reversal = StockDocument.CreateReversal(original, command.Notes, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (reversal.IsFailure)
            return reversal.Error;
        documents.Add(reversal.Value);
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ReversalCreated(logger, reversal.Value.Id, original.Id);
        return reversal.Value.Id;
    }

    [LoggerMessage(EventId = 5006, Level = LogLevel.Information, Message = "Reversal {ReversalId} of stock document {DocumentId} submitted")]
    private static partial void ReversalCreated(ILogger logger, Guid reversalId, Guid documentId);
}
