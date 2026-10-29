using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Finding and opening shifts; a shift's own rules are PosShiftDomainErrors.
public static class ShiftErrors
{
    public static readonly Error NotFound = Error.NotFound("Pos.Shift.NotFound", "The shift does not exist.");

    public static readonly Error NoOpenShift = Error.BusinessRule(
        "Pos.Shift.NoOpenShift", "You have no open shift on this terminal; open one first.");

    public static readonly Error TerminalBusy = Error.Conflict(
        "Pos.Shift.TerminalBusy", "The terminal already has an open shift; it must be closed first.");

    public static readonly Error CashierBusy = Error.Conflict(
        "Pos.Shift.CashierBusy", "You already have an open shift on another terminal; close it first.");
}
