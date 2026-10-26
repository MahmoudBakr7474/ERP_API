using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Application;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Tests;

// MasterEntity behaviour: temporal history, update stamps, optimistic concurrency.
[Collection(DatabaseCollection.Name)]
public sealed class MasterDataHistoryTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Row_inserted_inactive_stays_inactive()
    {
        await using var db = fx.NewDb();
        var unit = new Unit { Code = DatabaseFixture.Code("U"), NameAr = "علبة", IsActive = false };
        db.Set<Unit>().Add(unit);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        Assert.False((await db.Set<Unit>().SingleAsync(x => x.Id == unit.Id)).IsActive);
    }

    [Fact]
    public async Task Edit_keeps_the_old_version_in_history_and_stamps_update_time()
    {
        var productId = await CreateProductAsync();
        await using var db = fx.NewDb();
        var product = await db.Set<Product>().SingleAsync(x => x.Id == productId);
        product.NameAr = "صنف معدل";
        await db.SaveChangesAsync();

        var versions = await db.Set<Product>().TemporalAll().Where(x => x.Id == productId).Select(x => x.NameAr).ToListAsync();
        Assert.Equal(["صنف", "صنف معدل"], versions.Order());
        Assert.NotNull(product.UpdatedAtUtc);
    }

    [Fact]
    public async Task Stale_concurrent_edit_is_rejected()
    {
        var productId = await CreateProductAsync();
        await using var first = fx.NewDb();
        await using var second = fx.NewDb();
        var a = await first.Set<Product>().SingleAsync(x => x.Id == productId);
        var b = await second.Set<Product>().SingleAsync(x => x.Id == productId);

        a.NameEn = "First";
        await first.SaveChangesAsync();
        b.NameEn = "Second";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private async Task<Guid> CreateProductAsync()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        return await new ProductCommands(db).CreateProductAsync(
            DatabaseFixture.Code("SKU"), "صنف", unitId, null, ProductType.StockItem, TrackingMode.None, null, default);
    }
}
