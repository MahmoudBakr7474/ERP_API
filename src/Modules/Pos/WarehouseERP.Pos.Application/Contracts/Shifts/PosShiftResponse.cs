using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Shifts;

/// <summary>
/// A shift report: the drawer (float, cash sales received, cash refunds, cash in and out, expected and counted cash and the
/// difference), card totals kept apart from the drawer, counts, its cash movements and history. While open it is the X
/// report; once closed, the Z report.
/// </summary>
/// <param name="ExpectedCash">The drawer as it should be now (open) or when it closed.</param>
/// <param name="OpenCardIncidents">Card charges of this shift no sale was recorded for, still to be reversed.</param>
public sealed record PosShiftResponse(
    Guid Id, string Number, PosShiftStatus Status, Guid TerminalId, string TerminalCode, string TerminalName, Guid WarehouseId,
    string WarehouseCode, Guid BranchId, string BranchCode, Guid CashierUserId, string? CashierName, string CurrencyCode,
    DateTime OpenedAtUtc, decimal OpeningFloat, decimal CashSalesAmount, decimal CashRefundsAmount, decimal CashInAmount,
    decimal CashOutAmount, decimal ExpectedCash, decimal CardSalesAmount, decimal CardRefundsAmount, decimal SalesAmount,
    decimal RefundsAmount, int SalesCount, int ReturnsCount, DateTime? ClosedAtUtc, Guid? ClosedByUserId, string? ClosedByName,
    bool IsForceClosed, decimal? CountedCash, decimal? CashDifference, string? ClosingNote, int OpenCardIncidents,
    IReadOnlyList<PosCashMovementResponse> CashMovements, IReadOnlyList<PosShiftEventResponse> Events, string RowVersion);
