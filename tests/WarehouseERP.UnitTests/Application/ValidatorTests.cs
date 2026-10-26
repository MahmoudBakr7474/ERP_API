using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
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
}
