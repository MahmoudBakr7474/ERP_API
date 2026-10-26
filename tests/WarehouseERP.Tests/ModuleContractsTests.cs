using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Catalog;
using WarehouseERP.Catalog.Application;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization;
using WarehouseERP.Organization.Application;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Persistence;

namespace WarehouseERP.Tests;

// The contracts as another module sees them: resolved from DI, wired the way the API host wires them.
[Collection(DatabaseCollection.Name)]
public sealed class ModuleContractsTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Warehouse_directory_and_product_catalog_answer_through_the_shared_context()
    {
        var (branchId, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var provider = new ServiceCollection()
            .AddErpPersistence(fx.ConnectionString)
            .AddOrganizationModule()
            .AddCatalogModule()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var warehouseId = await services.GetRequiredService<WarehouseCommands>().CreateWarehouseAsync(
            branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true, null, default);
        var productId = await services.GetRequiredService<ProductCommands>().CreateProductAsync(
            DatabaseFixture.Code("SKU"), "صنف", unitId, null, ProductType.StockItem, TrackingMode.Batch, null, default);

        var warehouse = await services.GetRequiredService<IWarehouseDirectory>().FindAsync(warehouseId, default);
        var product = await services.GetRequiredService<IProductCatalog>().FindAsync(productId, default);

        Assert.Equal((branchId, WarehouseType.Physical, true), (warehouse!.BranchId, warehouse.Type, warehouse.AllowsSelling));
        Assert.Equal((unitId, TrackingMode.Batch), (product!.BaseUnitId, product.TrackingMode));
        Assert.Null(await services.GetRequiredService<IWarehouseDirectory>().FindAsync(Guid.NewGuid(), default));
    }
}
