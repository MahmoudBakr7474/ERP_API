using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.PostStockDocument;

// Direct posting of a draft whose type needs no approval, by someone who manages that type in its warehouse.
public sealed class PostStockDocumentHandler(
    IStockDocumentRepository documents, StockDocumentPosting posting, ICurrentUser currentUser)
    : ICommandHandler<PostStockDocumentCommand, PostedDocumentResponse>
{
    public async Task<Result<PostedDocumentResponse>> HandleAsync(PostStockDocumentCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, command.DocumentId, manage: true, ct);
        if (access.IsFailure)
            return access.Error;
        return await posting.PostAsync(command.DocumentId, command.IdempotencyKey, expected, byApproval: false, ct);
    }
}
