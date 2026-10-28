using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Application rules about purchase requests and ordering from them.
public static class PurchaseRequestErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.PurchaseRequest.NotFound", "The purchase request does not exist.");

    public static Error LineNotFound(int line) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.LineNotFound", $"Order line {line}: the purchase request line does not exist.");

    public static Error WarehouseMismatch(int line) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.WarehouseMismatch", $"Order line {line}: the request is for another warehouse.");

    public static Error ProductMismatch(int line) => Error.BusinessRule(
        "Purchasing.PurchaseRequest.ProductMismatch", $"Order line {line}: order the request line's product in its unit.");
}
