namespace WarehouseERP.Pos.Contracts;

/// <summary>A closed shift whose counted cash differed from what was expected.</summary>
public sealed record PosShiftCashDifference(
    Guid ShiftId, string Number, Guid BranchId, Guid TerminalId, DateTime ClosedAtUtc, decimal CashDifference, bool IsForceClosed);
