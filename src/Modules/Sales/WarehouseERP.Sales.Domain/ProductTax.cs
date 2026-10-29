using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// How VAT applies when a product is sold without anyone choosing it per line (a POS till): standard-rated at the
/// company's default VAT or at its own rate, zero-rated or exempt. One classification per product; a product without one
/// can't be sold at a till until someone classifies it. Master data, versioned in a temporal table; removing it
/// deactivates it, so its history stays.
/// </summary>
public sealed class ProductTax : MasterEntity
{
    private ProductTax() { } // EF Core

    /// <summary>Unique: one classification per product.</summary>
    public Guid ProductId { get; private set; }

    public SalesTaxTreatment TaxTreatment { get; private set; }

    /// <summary>A standard-rated product's own VAT rate; null takes the sales settings' default rate when sold. Always null otherwise.</summary>
    public decimal? TaxRate { get; private set; }

    public static Result<ProductTax> Create(Guid productId, SalesTaxTreatment taxTreatment, decimal? taxRate)
    {
        var tax = new ProductTax { ProductId = productId };
        var set = tax.Set(taxTreatment, taxRate);
        return set.IsFailure ? set.Error : tax;
    }

    /// <summary>Changes the classification and makes it active again if it was removed.</summary>
    public Result Set(SalesTaxTreatment taxTreatment, decimal? taxRate)
    {
        if (!Enum.IsDefined(taxTreatment))
            return ProductTaxDomainErrors.TreatmentInvalid;
        if (taxTreatment == SalesTaxTreatment.Standard
                ? taxRate is { } rate && (rate is <= 0 or > 100 || !Amounts.FitsScale(rate, Amounts.PercentScale))
                : taxRate is not null)
            return ProductTaxDomainErrors.RateInvalid;
        TaxTreatment = taxTreatment;
        TaxRate = taxRate;
        Activate();
        return Result.Success();
    }

    /// <summary>The rate a line of this product takes: its own, the default for standard-rated ones without one, 0 otherwise.</summary>
    public decimal RateWith(decimal defaultVatRate) =>
        TaxTreatment == SalesTaxTreatment.Standard ? TaxRate ?? defaultVatRate : 0m;
}
