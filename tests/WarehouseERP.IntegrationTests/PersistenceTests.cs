using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public sealed class PersistenceTests(DatabaseFixture fx)
{
    private static readonly Error SkuTaken = Error.Conflict("Test.Sku.Taken", "Taken.");

    [Fact]
    public async Task The_ef_model_matches_migration_01()
    {
        await using var db = fx.NewDb();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Known_unique_violation_becomes_its_business_error()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var sku = DatabaseFixture.Code("SKU");
        await using var db = fx.NewDb();
        db.Add(Product.Create(sku, "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        await db.SaveChangesAsync();

        db.Add(Product.Create(sku, "مكرر", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        var result = await DbFailures.SaveAsync(db, new Dictionary<string, Error> { ["IX_Products_SKU"] = SkuTaken }, default);

        Assert.Same(SkuTaken, result.Error);
    }

    [Fact]
    public async Task Unknown_unique_violation_is_not_disguised_as_a_known_one()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var sku = DatabaseFixture.Code("SKU");
        await using var db = fx.NewDb();
        db.Add(Product.Create(sku, "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        await db.SaveChangesAsync();

        db.Add(Product.Create(sku, "مكرر", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            DbFailures.SaveAsync(db, new Dictionary<string, Error> { ["IX_Something_Else"] = SkuTaken }, default));
        Assert.Equal("IX_Products_SKU", DbFailures.UniqueConstraintName(ex));
    }

    [Fact]
    public async Task Concurrency_conflict_becomes_the_common_error()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var product = Product.Create(DatabaseFixture.Code("SKU"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        await using (var setup = fx.NewDb())
        {
            setup.Add(product);
            await setup.SaveChangesAsync();
        }

        await using var first = fx.NewDb();
        await using var second = fx.NewDb();
        (await first.Set<Product>().SingleAsync(x => x.Id == product.Id)).Update("أول", null, categoryId: null);
        (await second.Set<Product>().SingleAsync(x => x.Id == product.Id)).Update("تاني", null, categoryId: null);
        await first.SaveChangesAsync();

        var result = await DbFailures.SaveAsync(second, new Dictionary<string, Error>(), default);
        Assert.Same(CommonErrors.ConcurrencyConflict, result.Error);
    }
}
