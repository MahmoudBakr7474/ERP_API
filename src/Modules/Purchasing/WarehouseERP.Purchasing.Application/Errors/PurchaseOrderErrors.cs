using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// Application rules about purchase orders; the order's own invariants are PurchaseOrderDomainErrors.
public static class PurchaseOrderErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.PurchaseOrder.NotFound", "The purchase order does not exist.");
}
