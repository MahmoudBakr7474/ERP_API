using WarehouseERP.Catalog.Application.Features.Categories.CreateCategory;
using WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;
using WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;
using WarehouseERP.Catalog.Application.Features.Units.CreateUnit;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.UnitTests.Domain;

public class CatalogRulesTests
{
    private static Product NewProduct(Guid? baseUnitId = null) =>
        Product.Create("SKU-1", "صنف", null, baseUnitId ?? Guid.NewGuid(), null, ProductType.StockItem, TrackingMode.None).Value;

    [Theory]
    [InlineData(BarcodeType.Ean13, "4006381333931", true)]
    [InlineData(BarcodeType.Ean13, "6221234567891", true)]
    [InlineData(BarcodeType.Ean13, "4006381333932", false)] // wrong check digit
    [InlineData(BarcodeType.Ean13, "400638133393", false)]  // 12 digits
    [InlineData(BarcodeType.Ean13, "40063813339AB", false)]
    [InlineData(BarcodeType.Gtin, "96385074", true)]        // GTIN-8
    [InlineData(BarcodeType.Gtin, "036000291452", true)]    // GTIN-12 (UPC-A)
    [InlineData(BarcodeType.Gtin, "4006381333931", true)]   // GTIN-13
    [InlineData(BarcodeType.Gtin, "00012345600012", true)]  // GTIN-14
    [InlineData(BarcodeType.Gtin, "00012345600013", false)]
    [InlineData(BarcodeType.Gtin, "1234567890", false)]     // 10 digits is no GTIN
    [InlineData(BarcodeType.Internal, "ITEM-0001/A", true)] // no GS1 rules
    [InlineData(BarcodeType.Internal, "4006381333932", true)]
    [InlineData(BarcodeType.Internal, "has space", false)]
    [InlineData(BarcodeType.Internal, "صنف", false)]
    [InlineData(BarcodeType.Other, "SUP.778-X", true)]
    [InlineData(BarcodeType.Other, "", false)]
    public void Barcode_format_follows_gs1_only_for_ean13_and_gtin(BarcodeType type, string value, bool valid) =>
        Assert.Equal(valid, BarcodeFormat.IsValid(type, value));

    [Fact]
    public void Barcode_longer_than_its_column_is_refused() =>
        Assert.False(BarcodeFormat.IsValid(BarcodeType.Internal, new string('A', 101)));

    [Fact]
    public void A_product_gets_a_unit_once_and_only_while_active()
    {
        var product = NewProduct();
        var carton = Guid.NewGuid();

        var added = product.AddUnit(carton, 12m, baseUnitAllowsFraction: false);
        Assert.True(added.IsSuccess);
        Assert.Equal(12m, added.Value.ConversionFactor);
        Assert.Same(ProductUnitDomainErrors.UnitAlreadyAdded, product.AddUnit(carton, 6m, baseUnitAllowsFraction: false).Error);
        Assert.Same(ProductUnitDomainErrors.UnitAlreadyAdded, product.AddUnit(product.BaseUnitId, 1m, baseUnitAllowsFraction: false).Error);

        product.Deactivate();
        Assert.Same(ProductDomainErrors.NotActive, product.AddUnit(Guid.NewGuid(), 6m, baseUnitAllowsFraction: false).Error);
    }

    [Fact]
    public void A_base_unit_that_cant_be_split_needs_a_whole_conversion_factor()
    {
        var product = NewProduct();

        Assert.Same(ProductUnitDomainErrors.FactorMustBeWhole, product.AddUnit(Guid.NewGuid(), 0.5m, baseUnitAllowsFraction: false).Error);
        Assert.True(product.AddUnit(Guid.NewGuid(), 0.5m, baseUnitAllowsFraction: true).IsSuccess);
        Assert.True(product.AddUnit(Guid.NewGuid(), 24.000m, baseUnitAllowsFraction: false).IsSuccess);
    }

    [Fact]
    public void The_base_unit_stays_active_and_other_units_can_be_retired()
    {
        var product = NewProduct();
        var baseUnit = product.Units.Single();
        var carton = product.AddUnit(Guid.NewGuid(), 12m, baseUnitAllowsFraction: false).Value;

        Assert.Same(ProductUnitDomainErrors.BaseUnitMustStayActive, product.SetUnitActive(baseUnit, isActive: false).Error);
        Assert.True(baseUnit.IsActive);
        Assert.True(product.SetUnitActive(carton, isActive: false).IsSuccess);
        Assert.False(carton.IsActive);
        Assert.True(product.SetUnitActive(carton, isActive: true).IsSuccess);
        Assert.True(carton.IsActive);
    }

    [Fact]
    public void Barcodes_go_on_active_units_of_active_products_and_come_back_only_with_their_unit()
    {
        var product = NewProduct();
        var carton = product.AddUnit(Guid.NewGuid(), 12m, baseUnitAllowsFraction: false).Value;

        Assert.Same(ProductBarcodeDomainErrors.InvalidFormat, product.AddBarcode(carton, "4006381333932", BarcodeType.Ean13).Error);
        var barcode = product.AddBarcode(carton, "4006381333931", BarcodeType.Ean13).Value;
        Assert.Equal(("4006381333931", BarcodeType.Ean13), (barcode.Barcode, barcode.BarcodeType));
        Assert.Single(carton.Barcodes);

        Assert.True(product.SetBarcodeActive(barcode, isActive: false).IsSuccess);
        product.SetUnitActive(carton, isActive: false);
        Assert.Same(ProductUnitDomainErrors.NotActive, product.SetBarcodeActive(barcode, isActive: true).Error);
        Assert.Same(ProductUnitDomainErrors.NotActive, product.AddBarcode(carton, "ITEM-2", BarcodeType.Internal).Error);

        product.Deactivate();
        Assert.Same(ProductDomainErrors.NotActive, product.AddBarcode(product.Units.First(), "ITEM-3", BarcodeType.Internal).Error);
    }

    [Fact]
    public void A_unit_of_another_product_is_refused()
    {
        var product = NewProduct();
        var other = NewProduct();

        Assert.Throws<ArgumentException>(() => product.SetUnitActive(other.Units.Single(), isActive: false));
        Assert.Throws<ArgumentException>(() => product.AddBarcode(other.Units.Single(), "ITEM-1", BarcodeType.Internal));
    }

    [Fact]
    public void Updating_a_product_changes_names_and_category_only()
    {
        var product = NewProduct();
        var category = Guid.NewGuid();

        product.Update(" اسم جديد ", " New ", category);

        Assert.Equal(("اسم جديد", "New", category), (product.NameAr, product.NameEn, product.CategoryId));
        Assert.Equal("SKU-1", product.SKU);
        product.Update("اسم", null, categoryId: null);
        Assert.Null(product.CategoryId);
    }

    [Fact]
    public void Unit_and_category_keep_their_codes()
    {
        var unit = Unit.Create("KG", "كيلو", null, allowsFraction: false);
        unit.Update(" كيلوجرام ", " Kilogram ", allowsFraction: true);
        Assert.Equal(("KG", "كيلوجرام", "Kilogram", true), (unit.Code, unit.NameAr, unit.NameEn, unit.AllowsFraction));

        var category = Category.Create("FOOD", "أغذية", null, parentId: null);
        var parent = Guid.NewGuid();
        category.Rename(" مواد غذائية ", " ");
        category.MoveTo(parent);
        Assert.Equal(("FOOD", "مواد غذائية", (string?)null, (Guid?)parent), (category.Code, category.NameAr, category.NameEn, category.ParentId));
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("0.5", true)]
    [InlineData("0.12345678", true)]
    [InlineData("0.123456789", false)] // nine decimals would be rounded by decimal(18, 8)
    [InlineData("12345678901", false)] // eleven whole digits
    [InlineData("0", false)]
    [InlineData("-1", false)]
    public void Conversion_factor_fits_its_column_and_is_positive(string factor, bool valid)
    {
        var command = new AddProductUnitCommand(Guid.NewGuid(), Guid.NewGuid(), decimal.Parse(factor, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(valid, new AddProductUnitValidator().Validate(command).IsValid);
    }

    [Theory]
    [InlineData(BarcodeType.Ean13, "4006381333931", true)]
    [InlineData(BarcodeType.Ean13, "4006381333930", false)]
    [InlineData(BarcodeType.Internal, "4006381333930", true)]
    [InlineData(BarcodeType.Internal, "two words", false)]
    public void Barcode_validator_reports_a_bad_value_on_the_field(BarcodeType type, string value, bool valid)
    {
        var result = new AddProductBarcodeValidator().Validate(new AddProductBarcodeCommand(Guid.NewGuid(), Guid.NewGuid(), value, type));

        Assert.Equal(valid, result.IsValid);
        if (!valid)
            Assert.Equal(("Barcode", "BarcodeFormatValidator"), (result.Errors.Single().PropertyName, result.Errors.Single().ErrorCode));
    }

    [Theory]
    [InlineData("PCS", true)]
    [InlineData("BOX_12", true)]
    [InlineData("قطعة", false)]
    [InlineData("TOO-LONG-UNIT-CODE-XYZ", false)] // 22 characters
    public void Unit_code_is_short_ascii(string code, bool valid) =>
        Assert.Equal(valid, new CreateUnitValidator().Validate(new CreateUnitCommand(code, "قطعة", null, AllowsFraction: false)).IsValid);

    [Fact]
    public void Category_code_is_ascii_and_an_empty_parent_id_is_refused()
    {
        var validator = new CreateCategoryValidator();
        Assert.True(validator.Validate(new CreateCategoryCommand("FOOD-01", "أغذية", null, null)).IsValid);
        Assert.False(validator.Validate(new CreateCategoryCommand("أغذية", "أغذية", null, null)).IsValid);
        Assert.False(validator.Validate(new CreateCategoryCommand("FOOD", "أغذية", null, Guid.Empty)).IsValid);
    }
}
