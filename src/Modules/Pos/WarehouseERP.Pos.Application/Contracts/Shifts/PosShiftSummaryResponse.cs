using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Shifts;

public sealed record PosShiftSummaryResponse(
    Guid Id, string Number, PosShiftStatus Status, Guid TerminalId, string TerminalCode, Guid WarehouseId, string WarehouseCode,
    Guid CashierUserId, string? CashierName, DateTime OpenedAtUtc, DateTime? ClosedAtUtc, decimal OpeningFloat,
    decimal ExpectedCash, decimal? CountedCash, decimal? CashDifference, decimal CardSalesAmount, int SalesCount,
    decimal SalesAmount, bool IsForceClosed);
