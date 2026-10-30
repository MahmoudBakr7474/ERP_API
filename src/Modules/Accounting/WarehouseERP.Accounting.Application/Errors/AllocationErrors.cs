using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class AllocationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Accounting.Allocation.NotFound", "The allocation does not exist.");

    public static readonly Error ItemNotFound = Error.BusinessRule(
        "Accounting.Allocation.ItemNotFound", "An open item does not exist.");

    public static readonly Error Busy = Error.Conflict(
        "Accounting.Allocation.Busy", "The customer's or supplier's items are being changed; try again in a moment.");
}
