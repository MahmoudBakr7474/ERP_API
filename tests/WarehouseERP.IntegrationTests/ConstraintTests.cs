using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.IntegrationTests;

// Rules the database enforces on its own, even for writes that skip the domain and the use cases.
[Collection(DatabaseCollection.Name)]
public sealed class ConstraintTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Selling_transit_warehouse_is_rejected_by_check()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        await DbAssert.SqlRejectedBy(db,
            $"INSERT INTO organization.Warehouses (Id, BranchId, Code, NameAr, WarehouseType, AllowsSelling) VALUES ({Guid.NewGuid()}, {branchId}, {DatabaseFixture.Code("TR")}, N'ترانزيت', 2, 1)",
            "CK_Warehouses_TransitNoSales");
    }

    [Fact]
    public async Task Duplicate_sku_is_rejected()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var sku = DatabaseFixture.Code("SKU");
        await using var db = fx.NewDb();
        db.Add(Product.Create(sku, "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        await db.SaveChangesAsync();

        db.Add(Product.Create(sku, "مكرر", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        await DbAssert.RejectedBy(db, "IX_Products_SKU");
    }

    [Fact]
    public async Task Duplicate_barcode_is_rejected()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var first = Product.Create(DatabaseFixture.Code("SKU"), "صنف 1", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        var second = Product.Create(DatabaseFixture.Code("SKU"), "صنف 2", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        db.AddRange(first, second);
        await db.SaveChangesAsync();
        var barcode = DatabaseFixture.Code("622");

        first.AddBarcode(first.Units.Single(), barcode, BarcodeType.Internal);
        await db.SaveChangesAsync();
        second.AddBarcode(second.Units.Single(), barcode, BarcodeType.Internal);
        await DbAssert.RejectedBy(db, "IX_ProductBarcodes_Barcode");
    }

    [Fact]
    public async Task External_code_cannot_point_at_another_products_unit()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var product = Product.Create(DatabaseFixture.Code("SKU"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        var other = Product.Create(DatabaseFixture.Code("SKU"), "صنف تاني", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        db.AddRange(product, other);
        await db.SaveChangesAsync();

        db.Add(new ExternalItemCode
        {
            ProductId = product.Id, ProductUnitId = other.Units.Single().Id, CodeType = ExternalCodeType.EGS, Code = DatabaseFixture.Code("EG"),
        });
        await DbAssert.RejectedBy(db, "FK_ExternalItemCodes_ProductUnits_ProductUnitId_ProductId");
    }

    [Fact]
    public async Task Branch_with_warehouses_cannot_be_deleted()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        db.Add(Warehouse.Create(branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true).Value);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        db.Remove(await db.Set<Branch>().SingleAsync(x => x.Id == branchId));
        await DbAssert.RejectedBy(db, "FK_Warehouses_Branches_BranchId");
    }
}
