using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>What Catalog said about a line's product unit when the line was entered.</summary>
/// <param name="ConversionFactor">Base units in one unit of this product unit.</param>
/// <param name="UnitAllowsFraction">Whether a quantity in this unit may have decimals.</param>
/// <param name="BaseUnitAllowsFraction">Whether a quantity in the product's base unit may have decimals.</param>
public sealed record ProductUnitFacts(
    Guid ProductId, Guid ProductUnitId, Guid UnitId, decimal ConversionFactor, bool UnitAllowsFraction, bool BaseUnitAllowsFraction)
{
    /// <summary>
    /// A positive quantity of at most 4 decimals, whole where the unit can't be split, whose base quantity is exact (and
    /// whole where the base unit can't be split): Inventory will be able to post it.
    /// </summary>
    public Result CheckQuantity(decimal quantity, int line)
    {
        if (quantity <= 0 || quantity > Amounts.MaxQuantity || !Amounts.FitsScale(quantity, Amounts.QuantityScale))
            return PurchaseLineDomainErrors.QuantityInvalid(line);
        if (!UnitAllowsFraction && !Amounts.IsWhole(quantity))
            return PurchaseLineDomainErrors.QuantityMustBeWhole(line);
        var baseQuantity = quantity * ConversionFactor;
        if (!Amounts.FitsScale(baseQuantity, Amounts.QuantityScale) || (!BaseUnitAllowsFraction && !Amounts.IsWhole(baseQuantity)))
            return PurchaseLineDomainErrors.ConversionNotExact(line);
        return Result.Success();
    }
}
