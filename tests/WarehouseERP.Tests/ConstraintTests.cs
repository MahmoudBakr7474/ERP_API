using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Application;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Application;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Tests;

// Rules the database enforces on its own, even for writes that skip the module commands.
[Collection(DatabaseCollection.Name)]
public sealed class ConstraintTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Selling_transit_warehouse_is_rejected_by_check()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        db.Set<Warehouse>().Add(new Warehouse
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
        await new ProductCommands(db).CreateProductAsync(sku, "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);

        db.Set<Product>().Add(new Product { SKU = sku, NameAr = "مكرر", BaseUnitId = unitId });
        await DbAssert.RejectedBy(db, "IX_Products_SKU");
    }

    [Fact]
    public async Task Duplicate_barcode_is_rejected()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var products = new ProductCommands(db);
        var first = await products.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف 1", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var second = await products.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف 2", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var units = await db.Set<ProductUnit>().Where(x => x.ProductId == first || x.ProductId == second).ToListAsync();
        var barcode = DatabaseFixture.Code("622");

        db.Set<ProductBarcode>().Add(new ProductBarcode { ProductUnitId = units.Single(x => x.ProductId == first).Id, Barcode = barcode });
        await db.SaveChangesAsync();
        db.Set<ProductBarcode>().Add(new ProductBarcode { ProductUnitId = units.Single(x => x.ProductId == second).Id, Barcode = barcode });
        await DbAssert.RejectedBy(db, "IX_ProductBarcodes_Barcode");
    }

    [Fact]
    public async Task External_code_cannot_point_at_another_products_unit()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var products = new ProductCommands(db);
        var product = await products.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var other = await products.CreateProductAsync(DatabaseFixture.Code("SKU"), "صنف تاني", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
        var otherUnitId = await db.Set<ProductUnit>().Where(x => x.ProductId == other).Select(x => x.Id).SingleAsync();

        db.Set<ExternalItemCode>().Add(new ExternalItemCode
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
        await new WarehouseCommands(db).CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true, null, default);

        db.ChangeTracker.Clear();
        db.Set<Branch>().Remove(await db.Set<Branch>().SingleAsync(x => x.Id == branchId));
        await DbAssert.RejectedBy(db, "FK_Warehouses_Branches_BranchId");
    }
}
