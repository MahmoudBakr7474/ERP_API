using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// The company's selling price of one product unit, in the base currency and tax-exclusive: the one sales price list of
/// V1. Master data, versioned in a temporal table. Removing a price deactivates it, so its history stays; setting it again
/// reactivates it. Quotation and order lines copy the price when entered: changing it never touches them.
/// </summary>
public sealed class SalesPrice : MasterEntity
{
    private SalesPrice() { } // EF Core

    public Guid ProductId { get; private set; }

    /// <summary>Unique: one price per product unit.</summary>
    public Guid ProductUnitId { get; private set; }

    public decimal UnitPrice { get; private set; }

    public static Result<SalesPrice> Create(Guid productId, Guid productUnitId, decimal unitPrice)
    {
        var price = new SalesPrice { ProductId = productId, ProductUnitId = productUnitId };
        var set = price.Set(unitPrice);
        return set.IsFailure ? set.Error : price;
    }

    /// <summary>Changes the price and makes it active again if it was removed.</summary>
    public Result Set(decimal unitPrice)
    {
        if (unitPrice <= 0 || unitPrice > Amounts.MaxUnitPrice || !Amounts.FitsScale(unitPrice, Amounts.PriceScale))
            return SalesPriceDomainErrors.UnitPriceInvalid;
        UnitPrice = unitPrice;
        Activate();
        return Result.Success();
    }
}
