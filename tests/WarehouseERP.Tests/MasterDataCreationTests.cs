using Microsoft.EntityFrameworkCore;
using WarehouseERP.Application.MasterData;
using WarehouseERP.Domain.Catalog;
using WarehouseERP.Domain.Organization;

namespace WarehouseERP.Tests;

[Collection(DatabaseCollection.Name)]
public sealed class MasterDataCreationTests(DatabaseFixture fx)
{
    [Fact]
    public async Task New_warehouse_gets_exactly_one_default_location()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var warehouseId = await new MasterDataCreation(db).CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true, null, default);

        var defaults = await db.WarehouseLocations.Where(l => l.WarehouseId == warehouseId && l.IsDefault).ToListAsync();
        Assert.Equal("DEFAULT", Assert.Single(defaults).Code);
    }

    [Fact]
    public async Task Second_default_location_is_rejected()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var warehouseId = await new MasterDataCreation(db).CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true, null, default);

        db.WarehouseLocations.Add(new WarehouseLocation { WarehouseId = warehouseId, Code = "D2", NameAr = "تاني", IsDefault = true });
        await DbAssert.RejectedBy(db, "UX_WarehouseLocations_OneDefault");
    }

    [Fact]
    public async Task Service_refuses_a_selling_transit_warehouse()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        await Assert.ThrowsAsync<ArgumentException>(() => new MasterDataCreation(db).CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("TR"), "ترانزيت", null, WarehouseType.Transit, true, null, default));
    }

    [Fact]
    public async Task New_product_gets_its_base_unit_row_with_factor_one()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var productId = await new MasterDataCreation(db).CreateProductAsync(
            DatabaseFixture.Code("SKU"), "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);

        var units = await db.ProductUnits.Where(x => x.ProductId == productId).ToListAsync();
        var baseUnit = Assert.Single(units);
        Assert.Equal(unitId, baseUnit.UnitId);
        Assert.Equal(1m, baseUnit.ConversionFactor);
    }

    [Fact]
    public async Task Service_refuses_tracking_on_a_non_stock_product()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        await Assert.ThrowsAsync<ArgumentException>(() => new MasterDataCreation(db).CreateProductAsync(
            DatabaseFixture.Code("SKU"), "خدمة", unitId, null, ProductType.Service, TrackingMode.Serial, null, default));
    }
}
