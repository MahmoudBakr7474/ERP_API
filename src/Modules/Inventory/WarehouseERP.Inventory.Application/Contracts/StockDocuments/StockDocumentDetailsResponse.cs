using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.StockDocuments;

/// <param name="RequiresApproval">The policy the document was created under.</param>
/// <param name="ReversedByDocumentId">The reversal that undoes this document, while it is not cancelled.</param>
/// <param name="TransferId">The transfer that produced it (dispatch, receipt or loss adjustment); it can't be edited or reversed.</param>
/// <param name="CountId">The count whose approval produced it.</param>
/// <param name="CreatedByUserId">Its creator, who may not approve it.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record StockDocumentDetailsResponse(
    Guid Id, string DraftReference, string? Number, StockDocumentType DocumentType, StockDocumentStatus Status,
    Guid WarehouseId, string WarehouseCode, Guid BranchId, bool RequiresApproval, Guid? ReversalOfDocumentId,
    Guid? ReversedByDocumentId, Guid? TransferId, Guid? CountId, AdjustmentReason? AdjustmentReason, string? ExternalReference,
    string? Notes,
    DateTime CreatedAtUtc, Guid? CreatedByUserId, string? CreatedBy, DateTime? PostedAtUtc, string? PostedBy,
    string? ApprovedBy, string RowVersion, IReadOnlyList<StockDocumentLineResponse> Lines,
    IReadOnlyList<StockDocumentEventResponse> Events);
