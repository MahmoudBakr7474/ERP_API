using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.GetStockDocument;

public sealed class GetStockDocumentHandler(
    IStockDocumentRepository documents, IStockDocumentReadStore readStore, ICurrentUser currentUser)
    : IQueryHandler<GetStockDocumentQuery, StockDocumentDetailsResponse>
{
    public async Task<Result<StockDocumentDetailsResponse>> HandleAsync(GetStockDocumentQuery query, CancellationToken ct)
    {
        var access = await StockDocumentAccess.AuthorizeAsync(documents, currentUser, query.DocumentId, manage: false, ct);
        if (access.IsFailure)
            return access.Error;
        var details = await readStore.GetAsync(query.DocumentId, ct);
        if (details is null)
            return StockDocumentErrors.NotFound;
        return details;
    }
}
