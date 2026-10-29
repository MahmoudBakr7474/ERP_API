using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Products' tax classifications, which a till prices with; a classification's own invariants are ProductTaxDomainErrors.
public static class ProductTaxErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.ProductTax.NotFound", "This product has no tax classification.");

    public static readonly Error ProductNotFound = Error.BusinessRule(
        "Sales.ProductTax.ProductNotFound", "The product does not exist.");

    public static readonly Error ProductNotActive = Error.BusinessRule(
        "Sales.ProductTax.ProductNotActive", "The product is inactive.");

    public static readonly Error AlreadyClassified = Error.Conflict(
        "Sales.ProductTax.AlreadyClassified", "Someone classified this product at the same moment; reload and try again.");

    public static Error NotClassified(int line) => Error.BusinessRule(
        "Sales.ProductTax.NotClassified",
        $"Line {line}: the product has no tax classification; a user allowed to manage prices must classify it before it is sold.");
}
