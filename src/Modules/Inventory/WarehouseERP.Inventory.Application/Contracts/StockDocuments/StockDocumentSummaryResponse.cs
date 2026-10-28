using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <param name="DraftReference">DR-000123, from creation on.</param>
/// <param name="Number">The final number, once posted.</param>
public sealed record StockDocumentSummaryResponse(
    Guid Id, string DraftReference, string? Number, StockDocumentType DocumentType, StockDocumentStatus Status,
    Guid WarehouseId, string WarehouseCode, bool RequiresApproval, Guid? ReversalOfDocumentId, string? ExternalReference,
    int LineCount, DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, DateTime? PostedAtUtc);
