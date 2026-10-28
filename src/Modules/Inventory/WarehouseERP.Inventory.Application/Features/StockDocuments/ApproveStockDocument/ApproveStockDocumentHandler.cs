using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ApproveStockDocument;

// An approver (endpoint permission, warehouse scope) who did not create the submitted document approves it, which posts it.
public sealed class ApproveStockDocumentHandler(
    IStockDocumentRepository documents, StockDocumentPosting posting, ICurrentUser currentUser)
    : ICommandHandler<ApproveStockDocumentCommand, PostedDocumentResponse>
{
    public async Task<Result<PostedDocumentResponse>> HandleAsync(ApproveStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: false, ct);
        if (access.IsFailure)
            return access.Error;
        return await posting.PostAsync(command.DocumentId, command.IdempotencyKey, expected, byApproval: true, ct);
    }
}
