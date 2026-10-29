using System.Globalization;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesLineDomainErrors
{
    public static Error QuantityInvalid(int line) => Error.BusinessRule(
        "Sales.Line.QuantityInvalid", $"Line {line}: the quantity must be above 0 with at most 4 decimals.");

    public static Error QuantityMustBeWhole(int line) => Error.BusinessRule(
        "Sales.Line.QuantityMustBeWhole", $"Line {line}: this unit cannot be split; enter a whole quantity.");

    public static Error ConversionNotExact(int line) => Error.BusinessRule(
        "Sales.Line.ConversionNotExact",
        $"Line {line}: the quantity does not convert exactly to the base unit (whole when it cannot be split, otherwise at most 4 decimals).");

    public static Error UnitPriceInvalid(int line) => Error.BusinessRule(
        "Sales.Line.UnitPriceInvalid", $"Line {line}: the unit price must be above 0 with at most 8 decimals.");

    public static Error DiscountInvalid(int line) => Error.BusinessRule(
        "Sales.Line.DiscountInvalid", $"Line {line}: the discount runs from 0 to 100% with at most 4 decimals.");

    public static Error TaxTreatmentNotSupported(int line) => Error.BusinessRule(
        "Sales.Line.TaxTreatmentNotSupported", $"Line {line}: the tax treatment must be standard, zero-rated or exempt.");

    public static Error TaxRateInvalid(int line) => Error.BusinessRule(
        "Sales.Line.TaxRateInvalid",
        $"Line {line}: a standard-rated line takes a VAT rate above 0 and up to 100%; zero-rated and exempt lines take 0.");

    public static Error AmountTooLarge(int line) => Error.BusinessRule(
        "Sales.Line.AmountTooLarge",
        string.Create(CultureInfo.InvariantCulture, $"Line {line}: the line amount is above {Amounts.MaxLineAmount}."));
}
