using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// The price list and how quotation and order lines take their prices; a price's own invariants are SalesPriceDomainErrors.
public static class PriceErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Price.NotFound", "The price list has no price for this product unit.");

    public static readonly Error ProductUnitNotFound = Error.BusinessRule(
        "Sales.Price.ProductUnitNotFound", "The product unit does not exist.");

    public static readonly Error ProductUnitNotActive = Error.BusinessRule(
        "Sales.Price.ProductUnitNotActive", "The product or its unit is inactive.");

    public static readonly Error AlreadyPriced = Error.Conflict(
        "Sales.Price.AlreadyPriced", "Someone priced this product unit at the same moment; reload and try again.");

    public static Error PriceMissing(int line) => Error.BusinessRule(
        "Sales.Line.PriceMissing", $"Line {line}: the price list has no price for this unit; a user allowed to manage prices must set one.");

    public static Error ManualPriceNotAllowed(int line) => Error.Forbidden(
        "Sales.Line.ManualPriceNotAllowed",
        $"Line {line}: only a user allowed to manage prices may set a price other than the price list's.");

    public static Error TaxRateNotAllowed(int line) => Error.BusinessRule(
        "Sales.Line.TaxRateNotAllowed", $"Line {line}: a zero-rated or exempt line takes no VAT rate.");
}
