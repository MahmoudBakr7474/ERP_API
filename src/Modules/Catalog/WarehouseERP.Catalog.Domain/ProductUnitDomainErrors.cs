using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Domain;

public static class ProductUnitDomainErrors
{
    public static readonly Error UnitAlreadyAdded = Error.Conflict(
        "Catalog.ProductUnit.UnitAlreadyAdded", "The product already has this unit.");

    public static readonly Error FactorMustBeWhole = Error.BusinessRule(
        "Catalog.ProductUnit.FactorMustBeWhole",
        "The product's base unit can't be split, so the conversion factor must be a whole number.");

    public static readonly Error BaseUnitMustStayActive = Error.BusinessRule(
        "Catalog.ProductUnit.BaseUnitMustStayActive", "The product's base unit can't be deactivated.");

    public static readonly Error NotActive = Error.BusinessRule(
        "Catalog.ProductUnit.NotActive", "The product unit is inactive; activate it first.");
}
