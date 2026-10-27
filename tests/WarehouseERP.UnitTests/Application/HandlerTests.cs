using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.GetWarehouse;
using WarehouseERP.Organization.Application.Features.Warehouses.ListWarehouses;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.Application;

// Handlers against in-memory fakes of their ports: the use-case logic without a database.
public sealed class HandlerTests
{
    private static readonly CreateWarehouseCommand NewWarehouse = new(Guid.NewGuid(), "WH-1", "مخزن", null, WarehouseType.Physical, true);

    [Fact]
    public async Task Create_warehouse_refuses_an_inactive_branch_before_saving()
    {
        var repository = new FakeWarehouseRepository { BranchActive = false };

        var result = await new CreateWarehouseHandler(repository, new FakeUser(true), NullLogger<CreateWarehouseHandler>.Instance).HandleAsync(NewWarehouse, default);

        Assert.Same(BranchErrors.NotActive, result.Error);
        Assert.Null(repository.Added);
    }

    [Fact]
    public async Task Create_warehouse_refuses_a_taken_code()
    {
        var repository = new FakeWarehouseRepository { CodeExists = true };
        var result = await new CreateWarehouseHandler(repository, new FakeUser(true), NullLogger<CreateWarehouseHandler>.Instance).HandleAsync(NewWarehouse, default);
        Assert.Same(WarehouseErrors.CodeTaken, result.Error);
    }

    [Fact]
    public async Task Create_warehouse_saves_it_with_its_default_location_once()
    {
        var repository = new FakeWarehouseRepository();
        var result = await new CreateWarehouseHandler(repository, new FakeUser(true), NullLogger<CreateWarehouseHandler>.Instance).HandleAsync(NewWarehouse, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.Saves);
        Assert.Single(repository.Added!.Locations, l => l.IsDefault);
    }

    [Fact]
    public async Task Create_warehouse_returns_the_save_points_error()
    {
        var repository = new FakeWarehouseRepository { SaveResult = WarehouseErrors.CodeTaken };
        var result = await new CreateWarehouseHandler(repository, new FakeUser(true), NullLogger<CreateWarehouseHandler>.Instance).HandleAsync(NewWarehouse, default);
        Assert.Same(WarehouseErrors.CodeTaken, result.Error);
    }

    [Fact]
    public async Task Create_warehouse_needs_company_wide_access()
    {
        var repository = new FakeWarehouseRepository();
        var result = await new CreateWarehouseHandler(repository, new FakeUser(false, Guid.NewGuid()), NullLogger<CreateWarehouseHandler>.Instance)
            .HandleAsync(NewWarehouse, default);

        Assert.Same(CompanyErrors.CompanyWideAccessRequired, result.Error);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task Get_warehouse_checks_scope_before_looking_it_up()
    {
        var store = new FakeWarehouseReadStore();
        var user = new FakeUser(allWarehouses: false);

        var result = await new GetWarehouseHandler(store, user).HandleAsync(new GetWarehouseQuery(Guid.NewGuid()), default);

        Assert.Same(WarehouseErrors.AccessDenied, result.Error);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task Get_warehouse_in_scope_but_missing_is_not_found()
    {
        var id = Guid.NewGuid();
        var result = await new GetWarehouseHandler(new FakeWarehouseReadStore(), new FakeUser(false, id))
            .HandleAsync(new GetWarehouseQuery(id), default);
        Assert.Same(WarehouseErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task List_warehouses_restricts_to_the_users_scope_and_defaults_the_page_size()
    {
        var store = new FakeWarehouseReadStore();
        var scoped = Guid.NewGuid();
        var options = Options.Create(new PaginationOptions { DefaultPageSize = 7 });

        await new ListWarehousesHandler(store, new FakeUser(false, scoped), options).HandleAsync(new ListWarehousesQuery(1, null, null, null, null), default);
        Assert.Equal([scoped], store.LastFilter!.OnlyWarehouseIds!);
        Assert.Equal(7, store.LastPageSize);

        await new ListWarehousesHandler(store, new FakeUser(true), options).HandleAsync(new ListWarehousesQuery(1, 3, null, null, null), default);
        Assert.Null(store.LastFilter!.OnlyWarehouseIds);
        Assert.Equal(3, store.LastPageSize);
    }

    [Fact]
    public async Task Create_product_refuses_an_inactive_category()
    {
        var repository = new FakeProductRepository { CategoryActive = false };
        var command = new CreateProductCommand("SKU-1", "صنف", null, Guid.NewGuid(), Guid.NewGuid(), ProductType.StockItem, TrackingMode.None);

        var result = await new CreateProductHandler(repository, NullLogger<CreateProductHandler>.Instance).HandleAsync(command, default);

        Assert.Same(CategoryErrors.NotActive, result.Error);
    }

    [Fact]
    public async Task Update_product_that_does_not_exist_is_not_found()
    {
        var result = await new UpdateProductHandler(new FakeProductRepository(), NullLogger<UpdateProductHandler>.Instance)
            .HandleAsync(new UpdateProductCommand(Guid.NewGuid(), "اسم", null, "AAAAAAAAB9E="), default);
        Assert.Same(ProductErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Update_product_renames_with_the_callers_row_version_and_passes_a_conflict_through()
    {
        var product = Product.Create("SKU-1", "صنف", null, Guid.NewGuid(), null, ProductType.StockItem, TrackingMode.None).Value;
        var repository = new FakeProductRepository { Stored = product, SaveResult = CommonErrors.ConcurrencyConflict };

        var result = await new UpdateProductHandler(repository, NullLogger<UpdateProductHandler>.Instance)
            .HandleAsync(new UpdateProductCommand(product.Id, "اسم جديد", null, "AAAAAAAAB9E="), default);

        Assert.Same(CommonErrors.ConcurrencyConflict, result.Error);
        Assert.Equal("اسم جديد", product.NameAr);
        Assert.Equal(Convert.FromBase64String("AAAAAAAAB9E="), repository.ExpectedRowVersion);
    }

    private sealed class FakeUser(bool allWarehouses, params Guid[] warehouses) : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
        public bool HasAllWarehouses => allWarehouses;
        public IReadOnlySet<Guid> WarehouseIds { get; } = warehouses.ToHashSet();
    }

    private sealed class FakeWarehouseRepository : IWarehouseRepository
    {
        public bool BranchActive { get; init; } = true;
        public bool CodeExists { get; init; }
        public Result SaveResult { get; init; } = Result.Success();
        public Warehouse? Added { get; private set; }
        public int Saves { get; private set; }

        public Task<bool> IsBranchActiveAsync(Guid branchId, CancellationToken ct) => Task.FromResult(BranchActive);
        public Task<bool> CodeExistsAsync(Guid branchId, string code, CancellationToken ct) => Task.FromResult(CodeExists);

        public Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct)
        {
            Added = warehouse;
            Saves++;
            return Task.FromResult(SaveResult);
        }

        public Task<Warehouse?> GetForUpdateAsync(Guid warehouseId, byte[]? expectedRowVersion, CancellationToken ct) =>
            Task.FromResult<Warehouse?>(null);

        public Task<bool> LocationCodeExistsAsync(Guid warehouseId, string code, CancellationToken ct) => Task.FromResult(false);

        public Task<WarehouseLocation?> GetLocationForUpdateAsync(Guid warehouseId, Guid locationId, byte[]? expectedRowVersion, CancellationToken ct) =>
            Task.FromResult<WarehouseLocation?>(null);

        public Task<Result> SaveChangesAsync(CancellationToken ct) => Task.FromResult(SaveResult);
    }

    private sealed class FakeWarehouseReadStore : IWarehouseReadStore
    {
        public int Reads { get; private set; }
        public WarehouseListFilter? LastFilter { get; private set; }
        public int LastPageSize { get; private set; }

        public Task<WarehouseDetailsResponse?> GetAsync(Guid warehouseId, CancellationToken ct)
        {
            Reads++;
            return Task.FromResult<WarehouseDetailsResponse?>(null);
        }

        public Task<PagedResult<WarehouseSummaryResponse>> ListAsync(WarehouseListFilter filter, int page, int pageSize, CancellationToken ct)
        {
            (LastFilter, LastPageSize) = (filter, pageSize);
            return Task.FromResult(new PagedResult<WarehouseSummaryResponse>([], page, pageSize, 0));
        }

        public Task<bool> ExistsAsync(Guid warehouseId, CancellationToken ct) => Task.FromResult(false);

        public Task<WarehouseLocationResponse?> GetLocationAsync(Guid warehouseId, Guid locationId, CancellationToken ct) =>
            Task.FromResult<WarehouseLocationResponse?>(null);

        public Task<PagedResult<WarehouseLocationResponse>> ListLocationsAsync(
            Guid warehouseId, string? search, bool? isActive, int page, int pageSize, CancellationToken ct) =>
            Task.FromResult(new PagedResult<WarehouseLocationResponse>([], page, pageSize, 0));
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public bool CategoryActive { get; init; } = true;
        public Product? Stored { get; init; }
        public Result SaveResult { get; init; } = Result.Success();
        public byte[]? ExpectedRowVersion { get; private set; }

        public Task<bool> IsUnitActiveAsync(Guid unitId, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> IsCategoryActiveAsync(Guid categoryId, CancellationToken ct) => Task.FromResult(CategoryActive);
        public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) => Task.FromResult(false);
        public Task<Result> AddAsync(Product product, CancellationToken ct) => Task.FromResult(SaveResult);

        public Task<Product?> GetForUpdateAsync(Guid productId, byte[] expectedRowVersion, CancellationToken ct)
        {
            ExpectedRowVersion = expectedRowVersion;
            return Task.FromResult(Stored);
        }

        public Task<Result> SaveChangesAsync(CancellationToken ct) => Task.FromResult(SaveResult);
    }
}
