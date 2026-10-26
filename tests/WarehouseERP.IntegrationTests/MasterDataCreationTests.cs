using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.IntegrationTests;

// The creation use cases through the real pipeline (validation, logging, repositories, SQL Server).
[Collection(DatabaseCollection.Name)]
public sealed class MasterDataCreationTests(DatabaseFixture fx)
{
    private async Task<Result<Guid>> CreateWarehouseAsync(CreateWarehouseCommand command)
    {
        await using var services = fx.Services();
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateWarehouseCommand, Guid>>().HandleAsync(command, default);
    }

    private async Task<Result<Guid>> CreateProductAsync(CreateProductCommand command)
    {
        await using var services = fx.Services();
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>().HandleAsync(command, default);
    }

    [Fact]
    public async Task New_warehouse_gets_exactly_one_default_location()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var warehouseId = (await CreateWarehouseAsync(
            new(branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true))).Value;

        await using var db = fx.NewDb();
        var defaults = await db.Set<WarehouseLocation>().Where(l => l.WarehouseId == warehouseId && l.IsDefault).ToListAsync();
        Assert.Equal(Warehouse.DefaultLocationCode, Assert.Single(defaults).Code);
    }

    [Fact]
    public async Task Second_default_location_is_rejected()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var warehouseId = (await CreateWarehouseAsync(
            new(branchId, DatabaseFixture.Code("WH"), "مخزن", null, WarehouseType.Physical, true))).Value;

        // The domain offers no way to add a second default, so the database rule is tested directly.
        await using var db = fx.NewDb();
        await DbAssert.SqlRejectedBy(db,
            $"INSERT INTO organization.WarehouseLocations (Id, WarehouseId, Code, NameAr, IsDefault) VALUES ({Guid.NewGuid()}, {warehouseId}, 'D2', N'تاني', 1)",
            "UX_WarehouseLocations_OneDefault");
    }

    [Fact]
    public async Task Selling_transit_warehouse_is_refused_by_the_domain()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var result = await CreateWarehouseAsync(new(branchId, DatabaseFixture.Code("TR"), "ترانزيت", null, WarehouseType.Transit, true));
        Assert.Same(WarehouseDomainErrors.TransitCannotSell, result.Error);
    }

    [Fact]
    public async Task New_product_gets_its_base_unit_row_with_factor_one()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var productId = (await CreateProductAsync(
            new(DatabaseFixture.Code("SKU"), "صنف", null, unitId, null, ProductType.StockItem, TrackingMode.None))).Value;

        await using var db = fx.NewDb();
        var baseUnit = Assert.Single(await db.Set<ProductUnit>().Where(x => x.ProductId == productId).ToListAsync());
        Assert.Equal((unitId, 1m), (baseUnit.UnitId, baseUnit.ConversionFactor));
    }

    [Fact]
    public async Task Tracking_a_non_stock_product_is_refused_by_the_domain()
    {
        var (_, unitId) = await fx.SeedBranchAndUnitAsync();
        var result = await CreateProductAsync(
            new(DatabaseFixture.Code("SKU"), "خدمة", null, unitId, null, ProductType.Service, TrackingMode.Serial));
        Assert.Same(ProductDomainErrors.TrackingNeedsStockItem, result.Error);
    }
}
