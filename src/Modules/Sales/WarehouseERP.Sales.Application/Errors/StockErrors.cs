using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Inventory refused to reserve, release, deliver or take back: its error code and message pass through unchanged (for
// example Inventory.Stock.Insufficient), so clients translate them like any Inventory error. Line numbers match the sales
// document's.
public static class StockErrors
{
    public static Error Refused(StockReservationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Refused(outcome.ErrorCode, outcome.ErrorMessage, outcome.IsConflict);
    }

    public static Error Refused(SalesStockOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Refused(outcome.ErrorCode, outcome.ErrorMessage, outcome.IsConflict);
    }

    private static Error Refused(string? errorCode, string? errorMessage, bool isConflict)
    {
        var code = errorCode ?? "Inventory.Stock.Refused";
        var message = errorMessage ?? "Inventory refused the request.";
        return isConflict ? Error.Conflict(code, message) : Error.BusinessRule(code, message);
    }
}
