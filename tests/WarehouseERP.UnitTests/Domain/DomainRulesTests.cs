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
        Assert.Same(WarehouseDomainErrors.TransitCannotSell,
            Warehouse.Create(Guid.NewGuid(), "TR", "ترانزيت", null, WarehouseType.Transit, allowsSelling: true).Error);
        Assert.True(Warehouse.Create(Guid.NewGuid(), "TR", "ترانزيت", null, WarehouseType.Transit, allowsSelling: false).IsSuccess);
    }

    [Fact]
    public void Updating_a_warehouse_keeps_transit_from_selling_and_changes_only_names_and_selling()
    {
        var transit = Warehouse.Create(Guid.NewGuid(), "TR", "ترانزيت", null, WarehouseType.Transit, allowsSelling: false).Value;
        Assert.Same(WarehouseDomainErrors.TransitCannotSell, transit.Update("ترانزيت", null, allowsSelling: true).Error);

        var warehouse = Warehouse.Create(Guid.NewGuid(), "WH-1", "مخزن", null, WarehouseType.Physical, allowsSelling: true).Value;
        Assert.True(warehouse.Update(" مخزن الجملة ", " ", allowsSelling: false).IsSuccess);
        Assert.Equal(("WH-1", "مخزن الجملة", null, false), (warehouse.Code, warehouse.NameAr, warehouse.NameEn, warehouse.AllowsSelling));
    }

    [Fact]
    public void Locations_are_added_to_active_warehouses_only_and_the_default_stays_active()
    {
        var warehouse = Warehouse.Create(Guid.NewGuid(), "WH-1", "مخزن", null, WarehouseType.Physical, allowsSelling: true).Value;
        var location = warehouse.AddLocation(" A-01 ", "رف", null).Value;
        Assert.Equal(("A-01", false), (location.Code, location.IsDefault));
        Assert.Equal(2, warehouse.Locations.Count);

        var defaultLocation = warehouse.Locations.Single(l => l.IsDefault);
        Assert.Same(WarehouseLocationDomainErrors.DefaultMustStayActive, defaultLocation.SetActive(false).Error);
        Assert.True(location.SetActive(false).IsSuccess);
        Assert.False(location.IsActive);

        warehouse.Deactivate();
        Assert.Same(WarehouseDomainErrors.NotActive, warehouse.AddLocation("A-02", "رف", null).Error);
    }

    [Fact]
    public void Company_settings_change_but_code_and_base_currency_stay()
    {
        var company = Company.Create("CO", "الشركة", null, "egp", "Africa/Cairo", null);
        company.UpdateSettings(" شركة التوزيع ", "Distribution", "Asia/Riyadh", " ");

        Assert.Equal(("CO", "EGP", "شركة التوزيع", "Asia/Riyadh", null),
            (company.Code, company.BaseCurrencyCode, company.NameAr, company.TimeZoneId, company.TaxRegistrationNumber));
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
        Assert.Same(ProductDomainErrors.TrackingNeedsStockItem,
            Product.Create("SKU-1", "خدمة", null, Guid.NewGuid(), null, type, tracking).Error);
    }

    [Fact]
    public void Update_trims_and_keeps_an_optional_english_name_absent_when_blank()
    {
        var product = Product.Create("SKU-1", "صنف", "Item", Guid.NewGuid(), null, ProductType.StockItem, TrackingMode.None).Value;

        product.Update(" اسم جديد ", " ", categoryId: null);

        Assert.Equal(("اسم جديد", null), (product.NameAr, product.NameEn));
        Assert.Throws<ArgumentException>(() => product.Update(" ", null, categoryId: null));
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
