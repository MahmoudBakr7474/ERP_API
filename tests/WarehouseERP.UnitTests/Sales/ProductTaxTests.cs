using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.UnitTests.Sales;

// A product's tax classification, which a till prices it with: standard-rated at the default VAT or its own rate,
// zero-rated or exempt without a rate; a rate that doesn't fit its treatment is refused, never ignored.
public sealed class ProductTaxTests
{
    [Fact]
    public void Standard_rated_products_take_their_own_rate_or_the_default_and_the_others_take_none()
    {
        Assert.Equal(14m, ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Standard, null).Value.RateWith(14m));
        Assert.Equal(5m, ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Standard, 5m).Value.RateWith(14m));
        Assert.Equal(0m, ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.ZeroRated, null).Value.RateWith(14m));
        Assert.Equal(0m, ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Exempt, null).Value.RateWith(14m));
    }

    [Fact]
    public void A_rate_that_does_not_fit_the_treatment_is_refused()
    {
        Assert.Equal("Sales.ProductTax.RateInvalid", ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Exempt, 14m).Error!.Code);
        Assert.Equal("Sales.ProductTax.RateInvalid", ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.ZeroRated, 0m).Error!.Code);
        Assert.Equal("Sales.ProductTax.RateInvalid", ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Standard, 0m).Error!.Code);
        Assert.Equal("Sales.ProductTax.RateInvalid", ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Standard, 100.5m).Error!.Code);
        Assert.Equal("Sales.ProductTax.TreatmentInvalid", ProductTax.Create(Guid.NewGuid(), (SalesTaxTreatment)9, null).Error!.Code);
    }

    [Fact]
    public void Reclassifying_a_removed_product_brings_it_back()
    {
        var tax = ProductTax.Create(Guid.NewGuid(), SalesTaxTreatment.Standard, null).Value;
        tax.Deactivate();
        Assert.True(tax.Set(SalesTaxTreatment.Exempt, null).IsSuccess);
        Assert.Equal((true, SalesTaxTreatment.Exempt, (decimal?)null), (tax.IsActive, tax.TaxTreatment, tax.TaxRate));
    }
}
