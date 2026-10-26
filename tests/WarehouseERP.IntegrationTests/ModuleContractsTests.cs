using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Features.CreateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Application.Features.CreateWarehouse;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests;

// The contracts as another module sees them: resolved from DI, wired the way the API host wires them.
[Collection(DatabaseCollection.Name)]
public sealed class ModuleContractsTests(DatabaseFixture fx)
{
    [Fact]
    public async Task Warehouse_directory_and_product_catalog_answer_through_the_shared_context()
    {
        var (branchId, unitId) = await fx.SeedBranchAndUnitAsync();
        await using var provider = fx.Services();
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var warehouseId = (await services.GetRequiredService<ICommandHandler<CreateWarehouseCommand, Guid>>().HandleAsync(
            new(branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true), default)).Value;
        var productId = (await services.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>().HandleAsync(
            new(DatabaseFixture.Code("SKU"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.Batch), default)).Value;

        var warehouse = await services.GetRequiredService<IWarehouseDirectory>().FindAsync(warehouseId, default);
        var product = await services.GetRequiredService<IProductCatalog>().FindAsync(productId, default);

        Assert.Equal((branchId, WarehouseType.Physical, true), (warehouse!.BranchId, warehouse.Type, warehouse.AllowsSelling));
        Assert.Equal((unitId, TrackingMode.Batch), (product!.BaseUnitId, product.TrackingMode));
        Assert.Null(await services.GetRequiredService<IWarehouseDirectory>().FindAsync(Guid.NewGuid(), default));
    }
}
