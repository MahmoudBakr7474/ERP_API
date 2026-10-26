using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Domain;

public static class WarehouseDomainErrors
{
    public static readonly Error TransitCannotSell = Error.BusinessRule(
        "Organization.Warehouse.TransitCannotSell", "A transit warehouse can never allow selling.");
}
