using System.Net;
using System.Text.Json;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// Permission policies at the endpoint, warehouse scope in the handler.
[Collection(DatabaseCollection.Name)]
public sealed class SecurityTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Warehouses = "/api/organization/warehouses";
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private async Task<Guid> CreateWarehouseAsync(Guid branchId)
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.WarehousesCreate).PostJsonAsync(Warehouses,
            new { branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = WarehouseType.Physical, allowsSelling = true });
        return (await response.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Anonymous_caller_gets_401()
    {
        var response = await _api.CreateClient().GetAsync(Warehouses);
        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    [Fact]
    public async Task Missing_permission_gets_403()
    {
        var response = await _api.CreateClient().As(CatalogPermissions.ProductsRead).GetAsync(Warehouses);
        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }

    [Fact]
    public async Task Reading_a_warehouse_outside_the_users_scope_is_403()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var allowed = await CreateWarehouseAsync(branchId);
        var other = await CreateWarehouseAsync(branchId);
        var client = _api.CreateClient().As(OrganizationPermissions.WarehousesRead, warehouses: allowed.ToString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Warehouses}/{allowed}")).StatusCode);
        var response = await client.GetAsync($"{Warehouses}/{other}");
        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
    }

    [Fact]
    public async Task Out_of_scope_and_missing_warehouses_look_the_same()
    {
        var client = _api.CreateClient().As(OrganizationPermissions.WarehousesRead, warehouses: Guid.NewGuid().ToString());
        var response = await client.GetAsync($"{Warehouses}/{Guid.NewGuid()}");
        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
    }

    [Fact]
    public async Task List_shows_only_warehouses_in_scope()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var allowed = await CreateWarehouseAsync(branchId);
        await CreateWarehouseAsync(branchId);
        var client = _api.CreateClient().As(OrganizationPermissions.WarehousesRead, warehouses: allowed.ToString());

        var page = await (await client.GetAsync($"{Warehouses}?branchId={branchId}")).ReadAsync<JsonElement>();

        Assert.Equal(allowed, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Request_body_cannot_set_fields_the_command_does_not_have()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As($"{OrganizationPermissions.WarehousesCreate},{OrganizationPermissions.WarehousesRead}");
        var forcedId = Guid.NewGuid();

        var response = await client.PostJsonAsync(Warehouses, new
        {
            id = forcedId, isActive = false, createdByUserId = Guid.NewGuid(),
            branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = WarehouseType.Physical, allowsSelling = true,
        });

        var created = (await response.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotEqual(forcedId, created);
        var details = await (await client.GetAsync($"{Warehouses}/{created}")).ReadAsync<JsonElement>();
        Assert.True(details.GetProperty("isActive").GetBoolean());
    }
}
