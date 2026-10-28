using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchaseRequests;

/// <param name="DraftReference">DPR-000123, its reference before it is numbered.</param>
public sealed record PurchaseRequestSummaryResponse(
    Guid Id, string DraftReference, string? Number, PurchaseRequestStatus Status, Guid WarehouseId, string WarehouseCode,
    DateOnly? NeededByDate, int LineCount, DateTime CreatedAtUtc, string? CreatedBy);
