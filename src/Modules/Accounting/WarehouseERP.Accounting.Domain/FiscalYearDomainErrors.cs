using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

public static class FiscalYearDomainErrors
{
    public static readonly Error StartNotMonthStart = Error.BusinessRule(
        "Accounting.FiscalYear.StartNotMonthStart", "A fiscal year starts on the first day of a month.");

    public static readonly Error PeriodNotOpen = Error.BusinessRule(
        "Accounting.FiscalPeriod.NotOpen", "The period is already closed.");

    public static readonly Error PeriodNotClosed = Error.BusinessRule(
        "Accounting.FiscalPeriod.NotClosed", "The period is open.");

    public static readonly Error YearClosed = Error.BusinessRule(
        "Accounting.FiscalYear.Closed", "The fiscal year is closed; reopen the year first.");

    public static readonly Error YearNotClosed = Error.BusinessRule(
        "Accounting.FiscalYear.NotClosed", "The fiscal year is open.");

    public static readonly Error PeriodsStillOpen = Error.BusinessRule(
        "Accounting.FiscalYear.PeriodsStillOpen", "Close every period of the year before closing the year.");

    public static readonly Error ReasonRequired = Error.BusinessRule(
        "Accounting.FiscalYear.ReasonRequired", $"Reopening needs a reason of at most {FiscalYear.MaxNoteLength} characters.");
}
