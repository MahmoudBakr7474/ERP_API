using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application;

public static class CatalogErrors
{
    // A command naming a missing or inactive unit/category breaks a rule of the request (422);
    // 404 is kept for the resource the URL addresses.
    public static readonly Error UnitNotActive = Error.BusinessRule(
        "Catalog.Unit.NotActive", "The unit does not exist or is inactive.");

    public static readonly Error CategoryNotActive = Error.BusinessRule(
        "Catalog.Category.NotActive", "The category does not exist or is inactive.");

    public static readonly Error SkuTaken = Error.Conflict(
        "Catalog.Product.SkuTaken", "Another product already uses this SKU.");

    public static readonly Error ProductNotFound = Error.NotFound(
        "Catalog.Product.NotFound", "The product does not exist.");
}
