using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

// What purchasing documents refer to in Organization and Catalog.
public static class ReferenceErrors
{
    public static readonly Error CompanySetupRequired = Error.BusinessRule(
        "Purchasing.Reference.CompanySetupRequired", "Set up the company before purchasing.");

    public static readonly Error WarehouseNotFound = Error.BusinessRule(
        "Purchasing.Reference.WarehouseNotFound", "The warehouse does not exist.");

    public static readonly Error WarehouseNotActive = Error.BusinessRule(
        "Purchasing.Reference.WarehouseNotActive", "The warehouse is inactive.");

    public static readonly Error WarehouseIsTransit = Error.BusinessRule(
        "Purchasing.Reference.WarehouseIsTransit", "Goods are bought into a physical warehouse, not a transit one.");

    public static Error ProductNotFound(int line) => Error.BusinessRule(
        "Purchasing.Reference.ProductNotFound", $"Line {line}: the product does not exist.");

    public static Error ProductNotStockItem(int line) => Error.BusinessRule(
        "Purchasing.Reference.ProductNotStockItem", $"Line {line}: Purchasing V1 buys stock items only.");

    public static Error TrackingNotSupported(int line) => Error.BusinessRule(
        "Purchasing.Reference.TrackingNotSupported", $"Line {line}: batch- and serial-tracked products are not supported yet.");

    public static Error ProductNotActive(int line) => Error.BusinessRule(
        "Purchasing.Reference.ProductNotActive", $"Line {line}: the product is inactive.");

    public static Error ProductUnitNotFound(int line) => Error.BusinessRule(
        "Purchasing.Reference.ProductUnitNotFound", $"Line {line}: the unit is not one of the product's units.");

    public static Error ProductUnitNotActive(int line) => Error.BusinessRule(
        "Purchasing.Reference.ProductUnitNotActive", $"Line {line}: the unit is inactive.");

    public static Error LocationNotFound(int line) => Error.BusinessRule(
        "Purchasing.Reference.LocationNotFound", $"Line {line}: the location is not in the order's warehouse.");

    public static Error LocationNotActive(int line) => Error.BusinessRule(
        "Purchasing.Reference.LocationNotActive", $"Line {line}: the location is inactive.");
}
