using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;

public interface IStockDocumentReadStore
{
    Task<StockDocumentDetailsResponse?> GetAsync(Guid documentId, CancellationToken ct);

    /// <summary>Newest first.</summary>
    Task<PagedResult<StockDocumentSummaryResponse>> ListAsync(StockDocumentFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>A posted document's outcome, to answer a retried request the way the first one was answered.</summary>
    Task<PostedDocumentResponse?> FindPostedAsync(Guid documentId, CancellationToken ct);
}
