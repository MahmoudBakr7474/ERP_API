using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Who may act at a till or on POS records: the terminal's warehouse must be in the user's scope, and a shift belongs to its
// cashier.
public static class AccessErrors
{
    public static readonly Error WarehouseDenied = Error.Forbidden(
        "Pos.Access.WarehouseDenied", "You are not allowed to use this warehouse's tills or see its POS records.");

    public static readonly Error CompanyWideRequired = Error.Forbidden(
        "Pos.Access.CompanyWideRequired", "Changing the POS settings needs company-wide access.");

    public static readonly Error NotShiftCashier = Error.Forbidden(
        "Pos.Access.NotShiftCashier", "Only the shift's cashier may do this; a shift manager may close or record cash on another cashier's shift.");

    public static Error PermissionDenied(string permission) => Error.Forbidden(
        "Pos.Access.PermissionDenied", $"You need the {permission} permission.");
}
