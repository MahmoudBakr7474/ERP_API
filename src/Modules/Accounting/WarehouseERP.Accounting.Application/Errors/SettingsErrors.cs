using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class SettingsErrors
{
    public static readonly Error StartMonthLocked = Error.BusinessRule(
        "Accounting.Settings.StartMonthLocked", "Fiscal years exist, so the month they start in can't change.");

    public static readonly Error CutoverLocked = Error.BusinessRule(
        "Accounting.Settings.CutoverLocked", "A journal has posted, so the accounting start date can't change.");
}
