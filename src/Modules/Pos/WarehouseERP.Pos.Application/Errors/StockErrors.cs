using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Inventory refused to post a sale or a return: its error code and message pass through unchanged (for example
// Inventory.Stock.Insufficient), so clients translate them like any Inventory error. Line numbers match the POS document's.
public static class StockErrors
{
    public static Error Refused(PosStockOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var code = outcome.ErrorCode ?? "Inventory.Stock.Refused";
        var message = outcome.ErrorMessage ?? "Inventory refused the request.";
        return outcome.IsConflict ? Error.Conflict(code, message) : Error.BusinessRule(code, message);
    }
}
