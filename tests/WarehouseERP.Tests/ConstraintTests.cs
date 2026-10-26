using Microsoft.EntityFrameworkCore;
using WarehouseERP.Application.MasterData;
using WarehouseERP.Domain.Catalog;
using WarehouseERP.Domain.Organization;

namespace WarehouseERP.Tests;

// Rules the database enforces on its own, even for writes that skip the application services.
[Collection(DatabaseCollection.Name)]
public sealed class ConstraintTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Selling_transit_warehouse_is_rejected_by_check()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        db.Warehouses.Add(new Warehouse
        {
            BranchId = branchId, Code = DatabaseFixture.Code("TR"), NameAr = "ترانزيت",
            WarehouseType = WarehouseType.Transit, AllowsSelling = true
        });
        await DbAssert.RejectedBy(db, "CK_Warehouses_TransitNoSales");
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var sku = DatabaseFixture.Code("SKU");
        await using var db = fx.NewDb();
        await new MasterDataCreation(db).CreateProductAsync(sku, "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);

        db.Products.Add(new Product { SKU = sku, NameAr = "مكرر", BaseUnitId = unitId });
        await DbAssert.RejectedBy(db, "IX_Products_SKU");
    }

    [Fact]
    public async Task Duplicate_barcode_is_rejected()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var creation = new MasterDataCreation(db);
        var first = await creation.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف 1", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var second = await creation.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف 2", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var units = await db.ProductUnits.Where(x => x.ProductId == first || x.ProductId == second).ToListAsync();
        var barcode = DatabaseFixture.Code("622");

        db.ProductBarcodes.Add(new ProductBarcode { ProductUnitId = units.Single(x => x.ProductId == first).Id, Barcode = barcode });
        await db.SaveChangesAsync();
        db.ProductBarcodes.Add(new ProductBarcode { ProductUnitId = units.Single(x => x.ProductId == second).Id, Barcode = barcode });
        await DbAssert.RejectedBy(db, "IX_ProductBarcodes_Barcode");
    }

    [Fact]
    public async Task External_code_cannot_point_at_another_products_unit()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var creation = new MasterDataCreation(db);
        var product = await creation.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var other = await creation.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف تاني", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var otherUnitId = await db.ProductUnits.Where(x => x.ProductId == other).Select(x => x.Id).SingleAsync();

        db.ExternalItemCodes.Add(new ExternalItemCode
        {
            ProductId = product, ProductUnitId = otherUnitId, CodeType = ExternalCodeType.EGS, Code = DatabaseFixture.Code("EG")
        });
        await DbAssert.RejectedBy(db, "FK_ExternalItemCodes_ProductUnits_ProductUnitId_ProductId");
    }

    [Fact]
    public async Task Branch_with_warehouses_cannot_be_deleted()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        await new MasterDataCreation(db).CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true, null, default);

        db.ChangeTracker.Clear();
        db.Branches.Remove(await db.Branches.SingleAsync(x => x.Id == branchId));
        await DbAssert.RejectedBy(db, "FK_Warehouses_Branches_BranchId");
    }
}
