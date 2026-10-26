using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.IntegrationTests;

// MasterEntity behaviour with private setters: EF materialization, temporal history, audit stamps,
// optimistic concurrency.
[Collection(DatabaseCollection.Name)]
public sealed class MasterDataHistoryTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Row_inserted_inactive_stays_inactive()
    {
        await using var db = fx.NewDb();
        var unit = Unit.Create(DatabaseFixture.Code("U"), "علبة", null, allowsFraction: false);
        unit.Deactivate();
        db.Add(unit);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        Assert.False((await db.Set<Unit>().SingleAsync(x => x.Id == unit.Id)).IsActive);
    }

    [Fact]
    public async Task Edit_keeps_the_old_version_in_history_and_stamps_who_and_when()
    {
        var productId = await CreateProductAsync();
        var editor = new TestCurrentUser(Guid.NewGuid(), allWarehouses: true);
        await using var db = fx.NewDb(editor);
        var product = await db.Set<Product>().SingleAsync(x => x.Id == productId);
        product.Rename("صنف معدل", "Edited");
        await db.SaveChangesAsync();

        var versions = await db.Set<Product>().TemporalAll().Where(x => x.Id == productId).Select(x => x.NameAr).ToListAsync();
        Assert.Equal(["صنف", "صنف معدل"], versions.Order());
        db.ChangeTracker.Clear();
        var stored = await db.Set<Product>().SingleAsync(x => x.Id == productId);
        Assert.Equal((fx.Clock.GetUtcNow().UtcDateTime, editor.UserId), (stored.UpdatedAtUtc, stored.UpdatedByUserId));
        Assert.Equal(TestCurrentUser.AllAccess().UserId, stored.CreatedByUserId); // creation stamp untouched
    }

    [Fact]
    public async Task Stale_concurrent_edit_is_rejected()
    {
        var productId = await CreateProductAsync();
        await using var first = fx.NewDb();
        await using var second = fx.NewDb();
        var a = await first.Set<Product>().SingleAsync(x => x.Id == productId);
        var b = await second.Set<Product>().SingleAsync(x => x.Id == productId);

        a.Rename("أول", null);
        await first.SaveChangesAsync();
        b.Rename("تاني", null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private async Task<Guid> CreateProductAsync()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var product = Product.Create(DatabaseFixture.Code("SKU"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        db.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }
}
