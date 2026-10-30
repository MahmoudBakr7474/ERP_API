namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>A closed shift whose counted cash differed from the expected cash (negative: short).</summary>
public sealed record CashDifferenceResponse(
    Guid ShiftId, string Number, Guid BranchId, DateTime ClosedAtUtc, decimal CashDifference, bool IsForceClosed);
