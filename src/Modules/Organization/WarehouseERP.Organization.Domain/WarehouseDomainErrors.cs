using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Domain;

public static class WarehouseDomainErrors
{
    public static readonly Error TransitCannotSell = Error.BusinessRule(
        "Organization.Warehouse.TransitCannotSell", "A transit warehouse can never allow selling.");

    public static readonly Error NotActive = Error.BusinessRule(
        "Organization.Warehouse.NotActive", "The warehouse is inactive.");
}
