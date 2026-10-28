using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Transfers;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CancelStockDocument;

// A draft or a submitted document is abandoned; a posted one is undone only by a reversal. Cancelling a transfer's loss
// adjustment lets the transfer receive again. Returns the new row version.
public sealed partial class CancelStockDocumentHandler(
    IStockDocumentRepository documents, IStockTransferRepository transfers, ICurrentUser currentUser, TimeProvider clock,
    ILogger<CancelStockDocumentHandler> logger)
    : ICommandHandler<CancelStockDocumentCommand, string>
{
    public async Task<Result<string>> HandleAsync(CancelStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: true, ct);
        if (access.IsFailure)
            return access.Error;
        var document = await documents.GetForUpdateAsync(command.DocumentId, expected, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;

        var now = clock.GetUtcNow().UtcDateTime;
        var cancelled = document.Cancel(currentUser.UserId, now);
        if (cancelled.IsFailure)
            return cancelled.Error;
        if (document.TransferId is { } transferId && document.DocumentType == StockDocumentType.Adjustment)
        {
            var transfer = await transfers.GetForUpdateAsync(transferId, expectedRowVersion: null, ct);
            transfer?.AbortClosing(currentUser.UserId, now);
        }
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentCancelled(logger, document.Id);
        return Convert.ToBase64String(document.RowVersion);
    }

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Stock document {DocumentId} cancelled")]
    private static partial void DocumentCancelled(ILogger logger, Guid documentId);
}
