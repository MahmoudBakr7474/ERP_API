using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.DocumentPolicies;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.CreateStockDocument;

// A draft for one physical warehouse in the user's scope, with the permission of its type. It copies the type's
// approval policy now, so a later policy change leaves it alone. One save point.
public sealed partial class CreateStockDocumentHandler(
    IStockDocumentRepository documents, IDocumentPolicyRepository policies, StockReferences references,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateStockDocumentHandler> logger)
    : ICommandHandler<CreateStockDocumentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateStockDocumentCommand command, CancellationToken ct)
    {
        if (command.DocumentType == StockDocumentType.Reversal)
            return StockDocumentErrors.ReversalNotDrafted;
        if (!currentUser.HasPermission(StockDocumentAccess.ManagePermission(command.DocumentType)))
            return StockDocumentErrors.PermissionDenied;
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return StockErrors.WarehouseAccessDenied;

        var warehouse = await references.UsableWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var lines = await references.ResolveLinesAsync(command.DocumentType, command.WarehouseId, command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        if (command.DocumentType == StockDocumentType.OpeningBalance
            && await documents.HasPostedBeyondOpeningAsync(command.WarehouseId, ct))
            return StockDocumentErrors.OpeningClosed;

        var requiresApproval = await policies.RequiresApprovalAsync(command.DocumentType, ct);
        var created = StockDocument.CreateDraft(
            command.DocumentType, command.WarehouseId, warehouse.Value.BranchId, requiresApproval, command.AdjustmentReason,
            command.ExternalReference, command.Notes, lines.Value, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;

        documents.Add(created.Value);
        var saved = await documents.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        DocumentCreated(logger, created.Value.Id, command.DocumentType);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Stock document {DocumentId} ({DocumentType}) drafted")]
    private static partial void DocumentCreated(ILogger logger, Guid documentId, StockDocumentType documentType);
}
