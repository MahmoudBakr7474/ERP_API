using WarehouseERP.Inventory.Application.Contracts.Stock;

namespace WarehouseERP.Inventory.Application.Contracts.Counts;

/// <summary>What approving a count posted; a retry with the same key gets the same answer.</summary>
/// <param name="AdjustmentDocumentId">The count adjustment, when the count found differences.</param>
/// <param name="AdjustmentNumber">CNT-…, when it found differences.</param>
public sealed record StockCountApprovalResponse(
    Guid CountId, Guid? AdjustmentDocumentId, string? AdjustmentNumber, DateTime CompletedAtUtc,
    IReadOnlyList<OverReservationResponse> OverReservations);
