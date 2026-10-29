namespace WarehouseERP.Pos.Application.Features.Shifts.GetCurrentPosShift;

/// <summary>The caller's open shift at a terminal.</summary>
public sealed record GetCurrentPosShiftQuery(Guid TerminalId);
