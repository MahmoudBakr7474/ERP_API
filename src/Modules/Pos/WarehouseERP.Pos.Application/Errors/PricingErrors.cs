using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Sales refused to price a cart line: its error code and message pass through unchanged (for example
// Sales.Line.PriceMissing or Sales.ProductTax.NotClassified). Line numbers match the cart's.
public static class PricingErrors
{
    public static Error Refused(SalesPricingOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Error.BusinessRule(outcome.ErrorCode ?? "Sales.Pricing.Refused", outcome.ErrorMessage ?? "Sales refused to price the cart.");
    }
}
