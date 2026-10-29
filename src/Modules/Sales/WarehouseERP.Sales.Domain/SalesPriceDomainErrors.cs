using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesPriceDomainErrors
{
    public static readonly Error UnitPriceInvalid = Error.BusinessRule(
        "Sales.Price.UnitPriceInvalid", "A selling price is above 0 with at most 8 decimals.");
}
