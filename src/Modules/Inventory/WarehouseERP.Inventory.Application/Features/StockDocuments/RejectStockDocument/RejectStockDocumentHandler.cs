using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.RejectStockDocument;

// An approver (endpoint permission, warehouse scope) sends a submitted document back to draft with a reason.
public sealed partial class RejectStockDocumentHandler(
    IStockDocumentRepository documents, ICurrentUser currentUser, TimeProvider clock, ILogger<RejectStockDocumentHandler> logger)
    : ICommandHandler<RejectStockDocumentCommand, string>
{
    public async Task<Result<string>> HandleAsync(RejectStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: false, ct);
        if (access.IsFailure)
            return access.Error;
        var document = await documents.GetForUpdateAsync(command.DocumentId, expected, ct);
        if (document is null)
            return StockDocumentErrors.NotFound;

        var rejected = document.Reject(command.Reason, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (rejected.IsFailure)
            return rejected.Error;
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentRejected(logger, document.Id);
        return Convert.ToBase64String(document.RowVersion);
    }

    [LoggerMessage(EventId = 5005, Level = LogLevel.Information, Message = "Stock document {DocumentId} rejected")]
    private static partial void DocumentRejected(ILogger logger, Guid documentId);
}
