using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.UpdateStockDocument;

// Only a draft changes, and only from the version its editor read (409 otherwise). Returns the new row version.
public sealed partial class UpdateStockDocumentHandler(
    IStockDocumentRepository documents, StockReferences references, ICurrentUser currentUser,
    ILogger<UpdateStockDocumentHandler> logger)
    : ICommandHandler<UpdateStockDocumentCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: true, ct);
        if (access.IsFailure)
            return access.Error;
        var document = await documents.GetForUpdateAsync(command.DocumentId, expected, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;
        if (document.DocumentType == StockDocumentType.Reversal)
            return StockDocumentDomainErrors.ReversalLinesFixed;
        if (document.Status != StockDocumentStatus.Draft)
            return StockDocumentDomainErrors.NotDraft;

        var lines = await references.ResolveLinesAsync(document.DocumentType, document.WarehouseId, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = document.ReplaceDraft(command.AdjustmentReason, command.ExternalReference, command.Notes, lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentUpdated(logger, document.Id);
        return Convert.ToBase64String(document.RowVersion);
    }

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Stock document {DocumentId} draft updated")]
    private static partial void DocumentUpdated(ILogger logger, Guid documentId);
}
