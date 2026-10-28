using WarehouseERP.Inventory.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Inventory refused to post a receipt or return: its error code and message pass through unchanged (for example
// Inventory.Stock.Insufficient), so clients translate them like any Inventory error. Line numbers match the purchasing
// document's.
public static class StockPostingErrors
{
    public static Error Refused(PurchaseStockOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var code = outcome.ErrorCode ?? "Inventory.Stock.Refused";
        var message = outcome.ErrorMessage ?? "Inventory refused the posting.";
        return outcome.IsConflict ? Error.Conflict(code, message) : Error.BusinessRule(code, message);
    }
}
