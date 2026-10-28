using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.SubmitStockDocument;

// A draft that needs approval waits for an approver. Returns the new row version.
public sealed partial class SubmitStockDocumentHandler(
    IStockDocumentRepository documents, ICurrentUser currentUser, TimeProvider clock, ILogger<SubmitStockDocumentHandler> logger)
    : ICommandHandler<SubmitStockDocumentCommand, string>
{
    public async Task<Result<string>> HandleAsync(SubmitStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: true, ct);
        if (access.IsFailure)
            return access.Error;
        var document = await documents.GetForUpdateAsync(command.DocumentId, expected, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;

        var submitted = document.Submit(currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (submitted.IsFailure)
            return submitted.Error;
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentSubmitted(logger, document.Id);
        return Convert.ToBase64String(document.RowVersion);
    }

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Stock document {DocumentId} submitted for approval")]
    private static partial void DocumentSubmitted(ILogger logger, Guid documentId);
}
