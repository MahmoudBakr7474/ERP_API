using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about sales orders; the order's own invariants are SalesOrderDomainErrors.
public static class OrderErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Order.NotFound", "The sales order does not exist.");
}
