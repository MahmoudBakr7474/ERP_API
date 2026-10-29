namespace WarehouseERP.Pos.Application.Features.Shifts.ClosePosShift;

/// <summary>Closes a shift with the cash counted in its drawer.</summary>
/// <param name="Note">Needed when the count differs from the expected drawer, and when a manager closes someone else's shift.</param>
/// <param name="RowVersion">The shift's version the user saw; read it just before counting.</param>
public sealed record ClosePosShiftCommand(Guid ShiftId, decimal CountedCash, string? Note, string RowVersion);
