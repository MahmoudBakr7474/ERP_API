using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class AccountingSettingsDomainErrors
{
    public static readonly Error StartMonthInvalid = Error.BusinessRule(
        "Accounting.Settings.StartMonthInvalid", "Fiscal years start in a month from 1 (January) to 12 (December).");

    public static readonly Error AlreadyActivated = Error.Conflict(
        "Accounting.Settings.AlreadyActivated", "Automatic accounting is already active.");

    public static readonly Error CutoverNotStarted = Error.BusinessRule(
        "Accounting.Settings.CutoverNotStarted",
        "Automatic accounting starts only once the accounting start date has begun: the opening balances are checked against that moment.");

    public static readonly Error CutoverNotPeriodStart = Error.BusinessRule(
        "Accounting.Settings.CutoverNotPeriodStart", "The accounting start date is the first day of a month (the start of a period).");
}
