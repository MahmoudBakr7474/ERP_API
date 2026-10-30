using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class FiscalYearErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.FiscalYear.NotFound", "The fiscal year does not exist.");

    public static readonly Error PeriodNotFound = Error.NotFound(
        "Accounting.FiscalPeriod.NotFound", "The period does not exist.");

    public static readonly Error Exists = Error.Conflict(
        "Accounting.FiscalYear.Exists", "A fiscal year already covers these dates.");

    public static readonly Error NotContiguous = Error.BusinessRule(
        "Accounting.FiscalYear.NotContiguous", "A new fiscal year starts the day after the last one ends.");

    public static readonly Error EarlierYearOpen = Error.BusinessRule(
        "Accounting.FiscalYear.EarlierYearOpen", "Close the earlier fiscal years first.");

    public static readonly Error LaterYearClosed = Error.BusinessRule(
        "Accounting.FiscalYear.LaterYearClosed", "Reopen the later fiscal years first.");

    public static readonly Error Busy = Error.Conflict(
        "Accounting.FiscalYear.Busy", "The fiscal year or its period is being changed or posted into; try again in a moment.");
}
