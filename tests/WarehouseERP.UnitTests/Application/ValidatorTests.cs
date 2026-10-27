using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Application.Features.Branches.CreateBranch;
using WarehouseERP.Organization.Application.Features.Companies.SetUpCompany;
using WarehouseERP.Organization.Application.Features.Companies.UpdateCompany;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.CreateWarehouseLocation;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.UpdateWarehouse;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.UnitTests.Application;

public sealed class ValidatorTests
{
    private static CreateWarehouseCommand Warehouse(string code = "WH-1", string nameAr = "مخزن", WarehouseType type = WarehouseType.Physical) =>
        new(Guid.NewGuid(), code, nameAr, null, type, true);

    [Fact]
    public void Valid_warehouse_command_passes()
    {
        Assert.True(new CreateWarehouseValidator().Validate(Warehouse()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("مخزن")]          // varchar column: ASCII only
    [InlineData("WH 1")]
    [InlineData("WAREHOUSE-CODE-LONGER-THAN-30-X")] // 31 characters
    public void Warehouse_code_must_be_short_ascii(string code)
    {
        var errors = new CreateWarehouseValidator().Validate(Warehouse(code: code)).Errors;
        Assert.Equal("Code", Assert.Single(errors).PropertyName);
    }

    [Fact]
    public void Warehouse_type_must_be_a_known_value_and_name_is_bounded()
    {
        var errors = new CreateWarehouseValidator().Validate(Warehouse(nameAr: new string('x', 151), type: (WarehouseType)9)).Errors;
        Assert.Equal(["NameAr", "Type"], errors.Select(e => e.PropertyName).Order());
    }

    [Theory]
    [InlineData("SKU-1/A.b_2", true)]
    [InlineData("صنف", false)]
    [InlineData("", false)]
    public void Sku_is_ascii(string sku, bool valid)
    {
        var command = new CreateProductCommand(sku, "صنف", null, Guid.NewGuid(), null, ProductType.StockItem, TrackingMode.None);
        Assert.Equal(valid, new CreateProductValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Empty_category_id_is_invalid_but_no_category_is_fine()
    {
        CreateProductCommand With(Guid? categoryId) => new("SKU-1", "صنف", null, Guid.NewGuid(), categoryId, ProductType.StockItem, TrackingMode.None);

        Assert.True(new CreateProductValidator().Validate(With(null)).IsValid);
        Assert.False(new CreateProductValidator().Validate(With(Guid.Empty)).IsValid);
    }

    [Theory]
    [InlineData("AAAAAAAAB9E=", true)]   // 8 bytes
    [InlineData("AAAAAAAAAA==", false)]  // 7 bytes
    [InlineData("not-base64", false)]
    [InlineData("", false)]
    public void Row_version_must_be_the_8_byte_value_read_with_the_product(string rowVersion, bool valid)
    {
        var result = new UpdateProductValidator().Validate(new UpdateProductCommand(Guid.NewGuid(), "صنف", null, rowVersion));
        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("EGP", "Africa/Cairo", null, true)]
    [InlineData("egp", "Egypt Standard Time", "123-456-789", true)] // Windows ids work too
    [InlineData("EG1", "Africa/Cairo", null, false)]
    [InlineData("EGP", "Mars/Olympus", null, false)]
    [InlineData("EGP", "Africa/Cairo", "رقم", false)]           // varchar column: ASCII only
    public void Company_setup_needs_an_iso_currency_a_known_time_zone_and_an_ascii_tax_number(
        string currency, string timeZone, string? taxNumber, bool valid)
    {
        var command = new SetUpCompanyCommand("CO-1", "الشركة", null, currency, timeZone, taxNumber);
        Assert.Equal(valid, new SetUpCompanyValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Company_update_needs_a_row_version()
    {
        var errors = new UpdateCompanyValidator().Validate(new UpdateCompanyCommand("الشركة", null, "Africa/Cairo", null, "")).Errors;
        Assert.Equal("RowVersion", Assert.Single(errors).PropertyName);
    }

    [Theory]
    [InlineData("BR-1", true)]
    [InlineData("فرع", false)]
    [InlineData("BRANCH-CODE-LONGER-THAN-30-XYZW", false)] // 31 characters
    public void Branch_code_is_short_ascii(string code, bool valid)
    {
        Assert.Equal(valid, new CreateBranchValidator().Validate(new CreateBranchCommand(code, "فرع", null)).IsValid);
    }

    [Theory]
    [InlineData("A.01-02_3", true)]
    [InlineData("A 01", false)]
    [InlineData("", false)]
    public void Location_code_is_ascii_with_dots_allowed(string code, bool valid)
    {
        var command = new CreateWarehouseLocationCommand(Guid.NewGuid(), code, "رف", null);
        Assert.Equal(valid, new CreateWarehouseLocationValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Warehouse_update_bounds_the_name_and_checks_the_row_version()
    {
        var command = new UpdateWarehouseCommand(Guid.NewGuid(), new string('x', 151), null, true, "not-base64");
        var errors = new UpdateWarehouseValidator().Validate(command).Errors;
        Assert.Equal(["NameAr", "RowVersion"], errors.Select(e => e.PropertyName).Order());
    }
}
