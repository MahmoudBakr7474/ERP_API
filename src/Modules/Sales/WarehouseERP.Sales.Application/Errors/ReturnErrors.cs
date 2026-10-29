using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about customer returns; the return's own invariants are SalesReturnDomainErrors.
public static class ReturnErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Return.NotFound", "The customer return does not exist.");
}
