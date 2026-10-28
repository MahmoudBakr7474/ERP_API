using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.Counts;

/// <param name="Locations">The counted locations; empty when the whole warehouse is counted.</param>
/// <param name="CreatedByUserId">Its creator, who may not approve it.</param>
/// <param name="AdjustmentDocumentId">The count adjustment its approval posted, when it found differences.</param>
/// <param name="RowVersion">Send it back with the next change or action.</param>
public sealed record StockCountDetailsResponse(
    Guid Id, string Reference, StockCountStatus Status, Guid WarehouseId, string WarehouseCode, bool IsWholeWarehouse,
    IReadOnlyList<StockCountLocationResponse> Locations, string? Notes, DateTime CreatedAtUtc, Guid? CreatedByUserId,
    string? CreatedBy, DateTime? StartedAtUtc, string? StartedBy, DateTime? SubmittedAtUtc, string? ApprovedBy,
    DateTime? CompletedAtUtc, Guid? AdjustmentDocumentId, string? AdjustmentNumber, string RowVersion,
    IReadOnlyList<StockCountLineResponse> Lines, IReadOnlyList<StockCountEventResponse> Events);
