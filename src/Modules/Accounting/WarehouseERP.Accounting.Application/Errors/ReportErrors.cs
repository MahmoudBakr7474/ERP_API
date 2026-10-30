using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class ReportErrors
{
    public static readonly Error AccountNotFound = Error.NotFound(
        "Accounting.Report.AccountNotFound", "The account does not exist.");

    public static Error TooManyLines(int max) => Error.BusinessRule(
        "Accounting.Report.TooManyLines", $"The statement has more than {max} lines; choose a shorter date range or a branch.");
}
