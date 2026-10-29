using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Errors;

// What sales documents refer to in Organization and Catalog.
public static class ReferenceErrors
{
    public static readonly Error CompanySetupRequired = Error.BusinessRule(
        "Sales.Reference.CompanySetupRequired", "Set up the company before selling.");

    public static readonly Error WarehouseNotFound = Error.BusinessRule(
        "Sales.Reference.WarehouseNotFound", "The warehouse does not exist.");

    public static readonly Error WarehouseNotActive = Error.BusinessRule(
        "Sales.Reference.WarehouseNotActive", "The warehouse is inactive.");

    public static readonly Error WarehouseNotSelling = Error.BusinessRule(
        "Sales.Reference.WarehouseNotSelling", "Goods are sold from a physical warehouse that allows selling.");

    public static Error ProductNotFound(int line) => Error.BusinessRule(
        "Sales.Reference.ProductNotFound", $"Line {line}: the product does not exist.");

    public static Error ProductNotStockItem(int line) => Error.BusinessRule(
        "Sales.Reference.ProductNotStockItem", $"Line {line}: Sales V1 sells stock items only.");

    public static Error TrackingNotSupported(int line) => Error.BusinessRule(
        "Sales.Reference.TrackingNotSupported", $"Line {line}: batch- and serial-tracked products are not supported yet.");

    public static Error ProductNotActive(int line) => Error.BusinessRule(
        "Sales.Reference.ProductNotActive", $"Line {line}: the product is inactive.");

    public static Error ProductUnitNotFound(int line) => Error.BusinessRule(
        "Sales.Reference.ProductUnitNotFound", $"Line {line}: the unit is not one of the product's units.");

    public static Error ProductUnitNotActive(int line) => Error.BusinessRule(
        "Sales.Reference.ProductUnitNotActive", $"Line {line}: the unit is inactive.");

    public static Error LocationNotFound(int line) => Error.BusinessRule(
        "Sales.Reference.LocationNotFound", $"Line {line}: the location is not in the order's warehouse.");

    public static Error LocationNotActive(int line) => Error.BusinessRule(
        "Sales.Reference.LocationNotActive", $"Line {line}: the location is inactive.");
}
