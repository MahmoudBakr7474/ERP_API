using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// Application rules about deliveries; the delivery's own invariants are SalesDeliveryDomainErrors.
public static class DeliveryErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Sales.Delivery.NotFound", "The delivery does not exist.");
}
