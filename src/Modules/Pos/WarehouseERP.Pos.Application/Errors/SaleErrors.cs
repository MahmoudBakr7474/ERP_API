using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Checking out a cart: what it sells, its discounts and its customer; payment rules are PosSaleDomainErrors.
public static class SaleErrors
{
    public static readonly Error NotFound = Error.NotFound("Pos.Sale.NotFound", "The sale does not exist.");

    public static readonly Error CustomerNotFound = Error.BusinessRule("Pos.Sale.CustomerNotFound", "The customer does not exist.");

    public static readonly Error CustomerNotActive = Error.BusinessRule("Pos.Sale.CustomerNotActive", "The customer is inactive.");

    public static Error ProductNotFound(int line) => Error.BusinessRule(
        "Pos.Sale.ProductNotFound", $"Line {line}: the product does not exist.");

    public static Error ProductNotStockItem(int line) => Error.BusinessRule(
        "Pos.Sale.ProductNotStockItem", $"Line {line}: POS V1 sells stock items only.");

    public static Error TrackingNotSupported(int line) => Error.BusinessRule(
        "Pos.Sale.TrackingNotSupported", $"Line {line}: batch- and serial-tracked products are not supported yet.");

    public static Error ProductNotActive(int line) => Error.BusinessRule(
        "Pos.Sale.ProductNotActive", $"Line {line}: the product is inactive.");

    public static Error ProductUnitNotFound(int line) => Error.BusinessRule(
        "Pos.Sale.ProductUnitNotFound", $"Line {line}: the unit is not a unit of this product.");

    public static Error ProductUnitNotActive(int line) => Error.BusinessRule(
        "Pos.Sale.ProductUnitNotActive", $"Line {line}: the unit is inactive.");

    public static Error QuantityInvalid(int line) => Error.BusinessRule(
        "Pos.Sale.QuantityInvalid", $"Line {line}: sell a quantity above 0 with at most 4 decimals.");

    public static Error QuantityMustBeWhole(int line) => Error.BusinessRule(
        "Pos.Sale.QuantityMustBeWhole", $"Line {line}: this unit is sold in whole numbers only.");

    public static Error ConversionNotExact(int line) => Error.BusinessRule(
        "Pos.Sale.ConversionNotExact", $"Line {line}: the quantity does not convert exactly to the product's base unit.");

    public static Error DiscountNotAllowed(int line) => Error.Forbidden(
        "Pos.Sale.DiscountNotAllowed", $"Line {line}: you are not allowed to give discounts.");

    public static Error DiscountAboveMax(int line, decimal max) => Error.BusinessRule(
        "Pos.Sale.DiscountAboveMax",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: a till discount is at most {max}%."));

    /// <summary>
    /// The checkout was refused after card payments were confirmed on a card terminal: no sale, stock or payment was
    /// recorded, the charge must be reversed there, and an incident keeps its references.
    /// </summary>
    public static Error CardReversalRequired(Error refusal, string references, Guid? incidentId) => Error.BusinessRule(
        "Pos.Checkout.CardReversalRequired",
        $"The sale was not recorded ({refusal.Code}: {refusal.Message}). Reverse card payment {references} on the card terminal"
        + (incidentId is { } id ? $"; card incident {id} keeps its reference until it is resolved." : "; the card incident could not be saved, note the reference."));
}
