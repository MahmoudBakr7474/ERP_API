using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

// Rules every purchasing line follows: quantities, prices, discounts and tax.
public static class PurchaseLineDomainErrors
{
    public static Error QuantityInvalid(int line) => Error.BusinessRule(
        "Purchasing.Line.QuantityInvalid", $"Line {line}: the quantity must be positive, with at most 4 decimals.");

    public static Error QuantityMustBeWhole(int line) => Error.BusinessRule(
        "Purchasing.Line.QuantityMustBeWhole", $"Line {line}: this unit can't be split; enter a whole quantity.");

    public static Error ConversionNotExact(int line) => Error.BusinessRule(
        "Purchasing.Line.ConversionNotExact", $"Line {line}: the quantity does not convert exactly into the product's base unit.");

    public static Error UnitPriceInvalid(int line) => Error.BusinessRule(
        "Purchasing.Line.UnitPriceInvalid", $"Line {line}: the unit price must be zero or more, with at most 8 decimals.");

    public static Error DiscountInvalid(int line) => Error.BusinessRule(
        "Purchasing.Line.DiscountInvalid", $"Line {line}: the discount must be between 0 and 100 percent, with at most 4 decimals.");

    public static Error TaxRateInvalid(int line) => Error.BusinessRule(
        "Purchasing.Line.TaxRateInvalid", $"Line {line}: the tax rate must be between 0 and 100 percent, with at most 4 decimals.");

    public static Error NonRecoverableTaxNotSupported(int line) => Error.BusinessRule(
        "Purchasing.Line.NonRecoverableTaxNotSupported",
        $"Line {line}: non-recoverable tax would belong in inventory cost, which V1 does not support. Enter recoverable tax only.");

    public static Error AmountTooLarge(int line) => Error.BusinessRule(
        "Purchasing.Line.AmountTooLarge", $"Line {line}: the line amount is too large.");
}
