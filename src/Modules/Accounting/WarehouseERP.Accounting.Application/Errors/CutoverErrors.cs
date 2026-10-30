using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class CutoverErrors
{
    public static Error NotReady(IEnumerable<string> failedChecks) => Error.BusinessRule(
        "Accounting.Cutover.NotReady", $"The cutover is not ready; these checks fail: {string.Join(", ", failedChecks)}.");
}
