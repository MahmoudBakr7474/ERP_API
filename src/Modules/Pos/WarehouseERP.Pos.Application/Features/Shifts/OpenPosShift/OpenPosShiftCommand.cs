namespace WarehouseERP.Pos.Application.Features.Shifts.OpenPosShift;

/// <summary>Opens the caller's shift at a terminal with the float counted into its drawer.</summary>
public sealed record OpenPosShiftCommand(Guid TerminalId, decimal OpeningFloat);
