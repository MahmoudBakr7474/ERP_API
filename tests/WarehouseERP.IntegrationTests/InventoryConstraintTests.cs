using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace WarehouseERP.IntegrationTests;

// The database is the last guard of the ledger's invariants, whatever code writes to it.
[Collection(DatabaseCollection.Name)]
public sealed class InventoryConstraintTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Balances_and_availability_never_go_below_zero_and_reserved_may_exceed_on_hand()
    {
        await using var db = fx.NewDb();
        var (productId, warehouseId, locationId) = await SeedAsync();

        await DbAssert.SqlRejectedBy(db,
            $"INSERT INTO inventory.StockBalances (ProductId, WarehouseId, LocationId, LotId, StockStatus, Quantity, UpdatedAtUtc) VALUES ({productId}, {warehouseId}, {locationId}, NULL, 1, -1, SYSUTCDATETIME())",
            "CK_StockBalances_Quantity");
        await DbAssert.SqlRejectedBy(db,
            $"INSERT INTO inventory.ProductWarehouseStock (ProductId, WarehouseId, OnHandSellable, ReservedQuantity) VALUES ({productId}, {warehouseId}, -1, 0)",
            "CK_ProductWarehouseStock_OnHandSellable");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO inventory.ProductWarehouseStock (ProductId, WarehouseId, OnHandSellable, ReservedQuantity) VALUES ({productId}, {warehouseId}, 1, 5)");
    }

    [Fact]
    public async Task One_balance_row_per_product_location_and_status_while_lots_are_null()
    {
        await using var db = fx.NewDb();
        var (productId, warehouseId, locationId) = await SeedAsync();
        FormattableString insert =
            $"INSERT INTO inventory.StockBalances (ProductId, WarehouseId, LocationId, LotId, StockStatus, Quantity, UpdatedAtUtc) VALUES ({productId}, {warehouseId}, {locationId}, NULL, 1, 1, SYSUTCDATETIME())";

        await db.Database.ExecuteSqlInterpolatedAsync(insert);
        await DbAssert.SqlRejectedBy(db, insert, "UX_StockBalances_Key");
    }

    [Fact]
    public async Task A_balance_names_a_location_of_its_own_warehouse()
    {
        await using var db = fx.NewDb();
        var (productId, warehouseId, _) = await SeedAsync();
        var (_, _, foreignLocation) = await SeedAsync();

        await DbAssert.SqlRejectedBy(db,
            $"INSERT INTO inventory.StockBalances (ProductId, WarehouseId, LocationId, LotId, StockStatus, Quantity, UpdatedAtUtc) VALUES ({productId}, {warehouseId}, {foreignLocation}, NULL, 1, 1, SYSUTCDATETIME())",
            "FK_StockBalances_WarehouseLocations_WarehouseId_LocationId");
    }

    [Fact]
    public async Task Other_modules_expose_what_inventory_reads_through_their_contracts()
    {
        var (productId, warehouseId, locationId) = await SeedAsync();
        await using var services = fx.Services();
        await using var scope = services.CreateAsyncScope();

        var location = Assert.Single(await scope.ServiceProvider.GetRequiredService<IWarehouseLocationDirectory>().FindManyAsync([locationId], default));
        Assert.Equal((warehouseId, true, true), (location.WarehouseId, location.IsDefault, location.IsActive));
        Assert.Equal(locationId, (await scope.ServiceProvider.GetRequiredService<IWarehouseLocationDirectory>().FindDefaultAsync(warehouseId, default))!.Id);
        Assert.Equal("Africa/Cairo", (await scope.ServiceProvider.GetRequiredService<ICompanyDirectory>().GetAsync(default))!.TimeZoneId);
        var baseUnit = Assert.Single(await scope.ServiceProvider.GetRequiredService<IProductCatalog>().FindBaseUnitsAsync([productId], default));
        Assert.Equal((1m, true, false), (baseUnit.ConversionFactor, baseUnit.IsBase, baseUnit.BaseUnitAllowsFraction));
        Assert.Single(await scope.ServiceProvider.GetRequiredService<IProductCatalog>().FindManyAsync([productId, Guid.NewGuid()], default));
    }

    private async Task<(Guid ProductId, Guid WarehouseId, Guid LocationId)> SeedAsync()
    {
        var (branchId, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var db = fx.NewDb();
        var warehouse = Organization.Domain.Warehouse.Create(branchId, DatabaseFixture.Code("W"), "مخزن", null, WarehouseType.Physical, true).Value;
        var product = Catalog.Domain.Product.Create(DatabaseFixture.Code("P"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None).Value;
        db.AddRange(warehouse, product);
        await db.SaveChangesAsync();
        return (product.Id, warehouse.Id, warehouse.Locations.Single().Id);
    }
}
