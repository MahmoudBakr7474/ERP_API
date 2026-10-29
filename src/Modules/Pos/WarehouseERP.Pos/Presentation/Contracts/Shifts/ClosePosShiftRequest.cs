namespace WarehouseERP.Pos.Presentation.Contracts.Shifts;

/// <summary>The cash counted in the drawer, a note (required for a difference or another cashier's shift) and the shift's row version.</summary>
public sealed record ClosePosShiftRequest(decimal CountedCash, string? Note, string RowVersion);
