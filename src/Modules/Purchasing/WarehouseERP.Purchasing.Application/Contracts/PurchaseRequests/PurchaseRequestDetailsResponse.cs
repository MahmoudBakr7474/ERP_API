using WarehouseERP.Purchasing.Application.Contracts.Documents;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

/// <param name="RequiresApproval">The policy the request was created under.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record PurchaseRequestDetailsResponse(
    Guid Id, string DraftReference, string? Number, PurchaseRequestStatus Status, Guid WarehouseId, string WarehouseCode,
    Guid BranchId, bool RequiresApproval, DateOnly? NeededByDate, string? Notes, string? ClosedReason, DateTime CreatedAtUtc,
    Guid? CreatedByUserId, string? CreatedBy, string? SubmittedBy, string? ApprovedBy, string RowVersion,
    IReadOnlyList<PurchaseRequestLineResponse> Lines, IReadOnlyList<DocumentEventResponse> Events);
