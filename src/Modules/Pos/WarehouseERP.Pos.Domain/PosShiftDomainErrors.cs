using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosShiftDomainErrors
{
    public static readonly Error NotOpen = Error.BusinessRule(
        "Pos.Shift.NotOpen", "The shift is closed; open a new one.");

    public static readonly Error AmountInvalid = Error.BusinessRule(
        "Pos.Shift.AmountInvalid", "Enter an amount of money with no more decimals than the currency has.");

    public static readonly Error CashMovementTypeInvalid = Error.BusinessRule(
        "Pos.Shift.CashMovementTypeInvalid", "Cash is either put into or taken out of the drawer.");

    public static readonly Error ReasonRequired = Error.BusinessRule(
        "Pos.Shift.ReasonRequired", $"Say why, in at most {PosShift.MaxReasonLength} characters.");

    public static readonly Error NoteTooLong = Error.BusinessRule(
        "Pos.Shift.NoteTooLong", $"The closing note is at most {PosShift.MaxNoteLength} characters.");

    public static readonly Error ForceCloseNoteRequired = Error.BusinessRule(
        "Pos.Shift.ForceCloseNoteRequired", "Closing another cashier's shift needs a note saying why.");

    public static Error DrawerShort(decimal expectedCash) => Error.BusinessRule(
        "Pos.Shift.DrawerShort",
        string.Create(CultureInfo.InvariantCulture, $"The drawer should hold only {expectedCash}; no more cash can come out of it."));

    public static Error DifferenceNoteRequired(decimal difference) => Error.BusinessRule(
        "Pos.Shift.DifferenceNoteRequired",
        string.Create(CultureInfo.InvariantCulture, $"The counted cash differs from the expected drawer by {difference}; add a closing note."));
}
