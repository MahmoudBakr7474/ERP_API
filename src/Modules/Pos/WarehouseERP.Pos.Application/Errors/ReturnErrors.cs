using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Taking goods back against a POS sale; quantities and refunds are PosSaleDomainErrors and PosReturnDomainErrors.
public static class ReturnErrors
{
    public static readonly Error NotFound = Error.NotFound("Pos.Return.NotFound", "The return does not exist.");

    public static readonly Error SaleNotFound = Error.BusinessRule("Pos.Return.SaleNotFound", "The sale does not exist.");

    public static Error WindowClosed(int days) => Error.BusinessRule(
        "Pos.Return.WindowClosed", $"Goods may come back within {days} days of the sale; this sale is older.");
}
