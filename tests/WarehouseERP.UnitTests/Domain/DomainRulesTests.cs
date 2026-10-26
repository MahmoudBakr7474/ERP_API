using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.UnitTests.Domain;

public sealed class DomainRulesTests
{
    [Fact]
    public void New_warehouse_comes_with_exactly_one_default_location()
    {
        var warehouse = Warehouse.Create(Guid.NewGuid(), " WH-1 ", "مخزن", "  ", WarehouseType.Physical, allowsSelling: true).Value;

        var location = Assert.Single(warehouse.Locations);
        Assert.Equal((Warehouse.DefaultLocationCode, true), (location.Code, location.IsDefault));
        Assert.Equal(("WH-1", null), (warehouse.Code, warehouse.NameEn));
    }

    [Fact]
    public void Transit_warehouse_can_not_sell()
    {
        Assert.Same(WarehouseErrors.TransitCannotSell,
            Warehouse.Create(Guid.NewGuid(), "TR", "ترانزيت", null, WarehouseType.Transit, allowsSelling: true).Error);
        Assert.True(Warehouse.Create(Guid.NewGuid(), "TR", "ترانزيت", null, WarehouseType.Transit, allowsSelling: false).IsSuccess);
    }

    [Fact]
    public void New_product_comes_with_its_base_unit_row_at_factor_one()
    {
        var unitId = Guid.NewGuid();
        var product = Product.Create("SKU-1", "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.Batch).Value;

        var baseUnit = Assert.Single(product.Units);
        Assert.Equal((unitId, 1m), (baseUnit.UnitId, baseUnit.ConversionFactor));
        Assert.Equal(unitId, product.BaseUnitId);
    }

    [Theory]
    [InlineData(ProductType.Service, TrackingMode.Serial)]
    [InlineData(ProductType.NonStockItem, TrackingMode.Batch)]
    public void Only_stock_items_are_tracked(ProductType type, TrackingMode tracking)
    {
        Assert.Same(ProductErrors.TrackingNeedsStockItem,
            Product.Create("SKU-1", "خدمة", null, Guid.NewGuid(), null, type, tracking).Error);
    }

    [Fact]
    public void Rename_trims_and_keeps_an_optional_english_name_absent_when_blank()
    {
        var product = Product.Create("SKU-1", "صنف", "Item", Guid.NewGuid(), null, ProductType.StockItem, TrackingMode.None).Value;

        product.Rename(" اسم جديد ", " ");

        Assert.Equal(("اسم جديد", null), (product.NameAr, product.NameEn));
        Assert.Throws<ArgumentException>(() => product.Rename(" ", null));
    }

    [Fact]
    public void Deactivation_is_a_domain_method()
    {
        var unit = Unit.Create("PCS", "قطعة", null, allowsFraction: false);
        unit.Deactivate();
        Assert.False(unit.IsActive);
        unit.Activate();
        Assert.True(unit.IsActive);
    }
}
