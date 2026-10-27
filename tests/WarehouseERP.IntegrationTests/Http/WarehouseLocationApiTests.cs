using System.Net;
using System.Text.Json;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class WarehouseLocationApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Warehouses = "/api/organization/warehouses";
    private static readonly string AllPermissions = string.Join(',', OrganizationPermissions.All);
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private static string Locations(Guid warehouseId) => $"{Warehouses}/{warehouseId}/locations";

    private static async Task<Guid> IdOf(HttpResponseMessage created)
    {
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        return (await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateWarehouseAsync()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        return await IdOf(await Admin().PostJsonAsync(Warehouses,
            new { branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = "Physical", allowsSelling = true }));
    }

    private async Task<Guid> CreateLocationAsync(Guid warehouseId, string code) =>
        await IdOf(await Admin().PostJsonAsync(Locations(warehouseId), new { code, nameAr = "رف", nameEn = "Shelf" }));

    [Fact]
    public async Task Create_returns_201_and_the_default_location_lists_first()
    {
        var warehouse = await CreateWarehouseAsync();
        var client = Admin();

        var created = await client.PostJsonAsync(Locations(warehouse), new { code = "A.01-02", nameAr = " رف أ ", nameEn = (string?)null });
        var id = await IdOf(created);

        Assert.Equal($"{Locations(warehouse)}/{id}", created.Headers.Location!.OriginalString);
        var location = await (await client.GetAsync($"{Locations(warehouse)}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal(("A.01-02", "رف أ", false, true), (location.GetProperty("code").GetString(), location.GetProperty("nameAr").GetString(),
            location.GetProperty("isDefault").GetBoolean(), location.GetProperty("isActive").GetBoolean()));

        var list = await (await client.GetAsync(Locations(warehouse))).ReadAsync<JsonElement>();
        Assert.Equal([Warehouse.DefaultLocationCode, "A.01-02"],
            list.GetProperty("items").EnumerateArray().Select(l => l.GetProperty("code").GetString()));
        var details = await (await client.GetAsync($"{Warehouses}/{warehouse}")).ReadAsync<JsonElement>();
        Assert.Equal(2, details.GetProperty("locations").GetArrayLength());
    }

    [Fact]
    public async Task Codes_are_unique_per_warehouse_whatever_the_case_and_must_be_ascii()
    {
        var warehouse = await CreateWarehouseAsync();
        await CreateLocationAsync(warehouse, "BIN-1");
        await CreateLocationAsync(await CreateWarehouseAsync(), "BIN-1"); // another warehouse may reuse it

        await (await Admin().PostJsonAsync(Locations(warehouse), new { code = "bin-1", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.WarehouseLocation.CodeTaken");
        await (await Admin().PostJsonAsync(Locations(warehouse), new { code = "default", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.WarehouseLocation.CodeTaken");
        await (await Admin().PostJsonAsync(Locations(warehouse), new { code = "رف 1", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [Fact]
    public async Task Update_renames_keeps_the_code_and_a_stale_row_version_is_409()
    {
        var warehouse = await CreateWarehouseAsync();
        var id = await CreateLocationAsync(warehouse, "R-1");
        var client = Admin();
        var rowVersion = (await (await client.GetAsync($"{Locations(warehouse)}/{id}")).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();

        var updated = await client.PutJsonAsync($"{Locations(warehouse)}/{id}", new { nameAr = "رف التبريد", nameEn = "Cold shelf", rowVersion });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var location = await (await client.GetAsync($"{Locations(warehouse)}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal(("R-1", "رف التبريد"), (location.GetProperty("code").GetString(), location.GetProperty("nameAr").GetString()));
        await (await client.PutJsonAsync($"{Locations(warehouse)}/{id}", new { nameAr = "قديم", rowVersion }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
    }

    [Fact]
    public async Task The_default_location_stays_active_and_others_can_be_deactivated_and_filtered()
    {
        var warehouse = await CreateWarehouseAsync();
        var client = Admin();
        var list = await (await client.GetAsync(Locations(warehouse))).ReadAsync<JsonElement>();
        var defaultId = list.GetProperty("items")[0].GetProperty("id").GetGuid();
        var other = await CreateLocationAsync(warehouse, "R-2");

        await (await client.PostAsync($"{Locations(warehouse)}/{defaultId}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.WarehouseLocation.DefaultMustStayActive");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Locations(warehouse)}/{other}/deactivate", null)).StatusCode);

        var inactive = await (await client.GetAsync($"{Locations(warehouse)}?isActive=false")).ReadAsync<JsonElement>();
        Assert.Equal(other, Assert.Single(inactive.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Locations(warehouse)}/{other}/activate", null)).StatusCode);
        var searched = await (await client.GetAsync($"{Locations(warehouse)}?search=R-2&isActive=true")).ReadAsync<JsonElement>();
        Assert.Equal(other, Assert.Single(searched.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task An_inactive_warehouse_gets_no_new_locations()
    {
        var warehouse = await CreateWarehouseAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await Admin().PostAsync($"{Warehouses}/{warehouse}/deactivate", null)).StatusCode);

        await (await Admin().PostJsonAsync(Locations(warehouse), new { code = "R-3", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.NotActive");
    }

    [Fact]
    public async Task Locations_follow_the_warehouse_scope()
    {
        var mine = await CreateWarehouseAsync();
        var other = await CreateWarehouseAsync();
        var otherLocation = await CreateLocationAsync(other, "R-4");
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: mine.ToString());

        Assert.Equal(HttpStatusCode.OK, (await scoped.GetAsync(Locations(mine))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await scoped.PostJsonAsync(Locations(mine), new { code = "R-5", nameAr = "رف" })).StatusCode);
        await (await scoped.GetAsync(Locations(other)))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        await (await scoped.GetAsync($"{Locations(other)}/{otherLocation}"))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        await (await scoped.PostJsonAsync(Locations(other), new { code = "R-6", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        await (await scoped.PostAsync($"{Locations(other)}/{otherLocation}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        // Reading a location through a warehouse it doesn't belong to finds nothing.
        await (await scoped.GetAsync($"{Locations(mine)}/{otherLocation}"))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.WarehouseLocation.NotFound");
    }

    [Fact]
    public async Task Missing_warehouse_is_404_for_a_company_wide_user()
    {
        await (await Admin().GetAsync(Locations(Guid.NewGuid())))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Warehouse.NotFound");
        await (await Admin().PostJsonAsync(Locations(Guid.NewGuid()), new { code = "R-7", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Warehouse.NotFound");
    }

    [Fact]
    public async Task Changes_need_the_location_permissions()
    {
        var warehouse = await CreateWarehouseAsync();
        var reader = _api.CreateClient().As(OrganizationPermissions.WarehousesRead);

        await (await reader.PostJsonAsync(Locations(warehouse), new { code = "R-8", nameAr = "رف" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }
}
