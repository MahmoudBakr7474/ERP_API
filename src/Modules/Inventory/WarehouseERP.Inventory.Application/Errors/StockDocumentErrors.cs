using WarehouseERP.Inventory.Contracts;
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

    public static readonly Error TypeNotDrafted = Error.BusinessRule(
        "Inventory.StockDocument.TypeNotDrafted", "Documents of this type are produced and changed by their transfer or count only.");

    public static readonly Error ManagedByPurchasing = Error.BusinessRule(
        "Inventory.StockDocument.ManagedByPurchasing",
        "Purchase receipts and returns are posted by Purchasing; correct them there with a supplier return.");

    public static readonly Error ManagedBySales = Error.BusinessRule(
        "Inventory.StockDocument.ManagedBySales",
        "Sales deliveries and customer returns are posted by Sales; correct them there with a sales return.");

    public static readonly Error ManagedByPos = Error.BusinessRule(
        "Inventory.StockDocument.ManagedByPos",
        "POS sales and returns are posted by POS; correct them there with a POS return.");

    public static readonly Error SourceAlreadyPosted = Error.Conflict(
        "Inventory.StockDocument.SourceAlreadyPosted", "This purchasing, sales or POS document has already been posted to stock.");

    public static readonly Error WarehouseNotSelling = Error.BusinessRule(
        "Inventory.StockDocument.WarehouseNotSelling", "Nothing may be sold from this warehouse: it does not allow selling.");

    public static Error SoldLineNotFound(int line) => Error.BusinessRule(
        "Inventory.StockDocument.SoldLineNotFound",
        $"Line {line}: the goods must come back from a posted POS sale line of the same product and warehouse.");

    public static Error ReturnExceedsSale(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ReturnExceedsSale", $"Line {line}: more would come back than its sale line still has unreturned.");

    public static Error DeliveryLineNotFound(int line) => Error.BusinessRule(
        "Inventory.StockDocument.DeliveryLineNotFound",
        $"Line {line}: the goods must come back from a posted sales delivery line of the same product and warehouse.");

    public static Error ReturnExceedsDelivery(int line) => Error.BusinessRule(
        "Inventory.StockDocument.ReturnExceedsDelivery", $"Line {line}: more would come back than its delivery line still has unreturned.");

    /// <summary>Why a document of this type can't be drafted or changed through Inventory's document endpoints.</summary>
    public static Error NotManagedHere(StockDocumentType documentType) => documentType switch
    {
        StockDocumentType.PurchaseReceipt or StockDocumentType.PurchaseReturn => ManagedByPurchasing,
        StockDocumentType.SalesDelivery or StockDocumentType.SalesReturn => ManagedBySales,
        StockDocumentType.PosSale or StockDocumentType.PosReturn => ManagedByPos,
        _ => TypeNotDrafted,
    };

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
