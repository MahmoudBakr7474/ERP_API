using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A priced line's amounts, each rounded to the currency's minor unit: Gross = quantity x unit price; Discount = Gross x
/// discount %; Net = Gross - Discount; Tax = Net x tax rate; Total = Net + Tax. NetUnitPrice is the unit price after
/// discount (8 decimals): it is the provisional inventory cost of one unit and the price invoices are matched against.
/// Recoverable tax never enters cost.
/// </summary>
public sealed record LineAmounts(decimal Gross, decimal Discount, decimal Net, decimal Tax, decimal Total, decimal NetUnitPrice)
{
    public static Result<LineAmounts> Calculate(decimal quantity, LinePrice price, int currencyDecimals, int line)
    {
        ArgumentNullException.ThrowIfNull(price);
        if (price.TaxTreatment != TaxTreatment.Recoverable)
            return PurchaseLineDomainErrors.NonRecoverableTaxNotSupported(line);
        if (price.UnitPrice < 0 || price.UnitPrice > Amounts.MaxUnitPrice || !Amounts.FitsScale(price.UnitPrice, Amounts.PriceScale))
            return PurchaseLineDomainErrors.UnitPriceInvalid(line);
        if (price.DiscountPercent is < 0 or > 100 || !Amounts.FitsScale(price.DiscountPercent, Amounts.PercentScale))
            return PurchaseLineDomainErrors.DiscountInvalid(line);
        if (price.TaxRate is < 0 or > 100 || !Amounts.FitsScale(price.TaxRate, Amounts.PercentScale))
            return PurchaseLineDomainErrors.TaxRateInvalid(line);
        if (quantity * price.UnitPrice > Amounts.MaxLineAmount)
            return PurchaseLineDomainErrors.AmountTooLarge(line);

        var gross = Amounts.Round(quantity * price.UnitPrice, currencyDecimals);
        var discount = Amounts.Round(gross * price.DiscountPercent / 100m, currencyDecimals);
        var net = gross - discount;
        var tax = Amounts.Round(net * price.TaxRate / 100m, currencyDecimals);
        var netUnitPrice = Amounts.RoundPrice(price.UnitPrice * (100m - price.DiscountPercent) / 100m);
        return new LineAmounts(gross, discount, net, tax, net + tax, netUnitPrice);
    }
}
