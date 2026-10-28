using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments.ListStockDocuments;

/// <param name="Search">Matches the number, the draft reference or the external reference.</param>
public sealed record ListStockDocumentsQuery(
    int Page, int? PageSize, Guid? WarehouseId = null, StockDocumentType? DocumentType = null,
    StockDocumentStatus? Status = null, string? Search = null) : IPagedQuery;
