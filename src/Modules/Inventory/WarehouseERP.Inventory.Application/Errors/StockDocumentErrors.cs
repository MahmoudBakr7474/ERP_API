using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

// Application rules about stock documents; the document invariants themselves are StockDocumentDomainErrors.
public static class StockDocumentErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Inventory.StockDocument.NotFound", "The stock document does not exist.");

    public static readonly Error AccessDenied = Error.Forbidden(
        "Inventory.StockDocument.AccessDenied", "You are not allowed to access this document's warehouse.");

    public static readonly Error PermissionDenied = Error.Forbidden(
        "Inventory.StockDocument.PermissionDenied", "You are not allowed to manage documents of this type.");

    public static readonly Error ReversalNotDrafted = Error.BusinessRule(
        "Inventory.StockDocument.ReversalNotDrafted", "A reversal is created from the posted document it reverses.");

    public static readonly Error WarehouseNotFound = Error.BusinessRule(
        "Inventory.StockDocument.WarehouseNotFound", "The warehouse does not exist.");

    public static readonly Error WarehouseNotActive = Error.BusinessRule(
        "Inventory.StockDocument.WarehouseNotActive", "The warehouse is inactive.");

    public static readonly Error WarehouseIsTransit = Error.BusinessRule(
        "Inventory.StockDocument.WarehouseIsTransit", "A transit warehouse takes stock only through transfers.");

    public static readonly Error OpeningClosed = Error.BusinessRule(
        "Inventory.StockDocument.OpeningClosed", "Opening stock is entered before any other document posts in the warehouse.");

    public static readonly Error AlreadyReversed = Error.Conflict(
        "Inventory.StockDocument.AlreadyReversed", "This document already has a reversal.");

    public static readonly Error CompanySetupRequired = Error.BusinessRule(
        "Inventory.StockDocument.CompanySetupRequired", "Set up the company before posting documents.");

    public static Error ProductNotFound(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ProductNotFound", $"Line {line}: the product does not exist.");

    public static Error ProductNotStockItem(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ProductNotStockItem", $"Line {line}: only stock items are kept in stock.");

    public static Error TrackingNotSupported(int line) => Error.BusinessRule(
        "Inventory.StockDocument.TrackingNotSupported", $"Line {line}: batch- and serial-tracked products are not supported yet.");

    public static Error ProductNotActive(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ProductNotActive", $"Line {line}: the product is inactive; its stock can only go out.");

    public static Error ProductUnitNotFound(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ProductUnitNotFound", $"Line {line}: the unit is not one of the product's units.");

    public static Error ProductUnitNotActive(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ProductUnitNotActive", $"Line {line}: the unit is inactive.");

    public static Error LocationNotFound(int line) => Error.BusinessRule(
        "Inventory.StockDocument.LocationNotFound", $"Line {line}: the location is not in the document's warehouse.");

    public static Error LocationNotActive(int line) => Error.BusinessRule(
        "Inventory.StockDocument.LocationNotActive", $"Line {line}: the location is inactive.");

    public static Error DirectionRequired(int line) => Error.BusinessRule(
        "Inventory.StockDocument.DirectionRequired", $"Line {line}: say whether the adjustment adds or removes stock.");
}
