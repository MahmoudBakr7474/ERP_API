using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class WarehouseApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Route = "/api/organization/warehouses";
    private const string AllPermissions = $"{OrganizationPermissions.WarehousesCreate},{OrganizationPermissions.WarehousesRead}";
    private const string UpdatePermissions = $"{AllPermissions},{OrganizationPermissions.WarehousesUpdate}";
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static object NewWarehouse(Guid branchId, string? code = null, WarehouseType type = WarehouseType.Physical, bool allowsSelling = true) =>
        new { branchId, code = code ?? DatabaseFixture.Code("WH"), nameAr = "مخزن", nameEn = "Store", type, allowsSelling };

    [Fact]
    public async Task Create_returns_201_with_location_and_the_warehouse_reads_back_with_its_default_location()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);

        var created = await client.PostJsonAsync(Route, NewWarehouse(branchId));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal($"{Route}/{id}", created.Headers.Location!.OriginalString);

        var details = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal("Physical", details.GetProperty("type").GetString());
        var location = Assert.Single(details.GetProperty("locations").EnumerateArray());
        Assert.Equal((Warehouse.DefaultLocationCode, true), (location.GetProperty("code").GetString(), location.GetProperty("isDefault").GetBoolean()));
    }

    [Fact]
    public async Task Created_rows_are_stamped_with_the_caller_and_the_clock()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var created = await _api.CreateClient().As(AllPermissions).PostJsonAsync(Route, NewWarehouse(branchId));
        var id = (await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();

        await using var db = fx.NewDb();
        var row = await db.Set<Warehouse>().SingleAsync(x => x.Id == id);
        Assert.Equal((HttpTesting.UserId, fx.Clock.GetUtcNow().UtcDateTime), (row.CreatedByUserId!.Value, row.CreatedAtUtc));
    }

    [Fact]
    public async Task Invalid_input_is_400_with_field_errors_not_500()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var response = await _api.CreateClient().As(AllPermissions).PostJsonAsync(Route, NewWarehouse(branchId, code: ""));

        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var field = Assert.Single(problem.GetProperty("errors").EnumerateArray(), e => e.GetProperty("property").GetString() == "code");
        Assert.Equal("NotEmptyValidator", field.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Selling_transit_warehouse_is_422_with_the_domain_error_code()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var response = await _api.CreateClient().As(AllPermissions)
            .PostJsonAsync(Route, NewWarehouse(branchId, type: WarehouseType.Transit, allowsSelling: true));

        await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.TransitCannotSell");
    }

    [Fact]
    public async Task Unknown_branch_is_422()
    {
        var response = await _api.CreateClient().As(AllPermissions).PostJsonAsync(Route, NewWarehouse(Guid.NewGuid()));
        await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Branch.NotActive");
    }

    [Fact]
    public async Task Duplicate_code_in_a_branch_is_409()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        var code = DatabaseFixture.Code("WH");
        Assert.Equal(HttpStatusCode.Created, (await client.PostJsonAsync(Route, NewWarehouse(branchId, code))).StatusCode);

        var response = await client.PostJsonAsync(Route, NewWarehouse(branchId, code));
        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Warehouse.CodeTaken");
    }

    [Fact]
    public async Task Missing_warehouse_is_404()
    {
        var response = await _api.CreateClient().As(AllPermissions).GetAsync($"{Route}/{Guid.NewGuid()}");
        await response.AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Warehouse.NotFound");
    }

    [Fact]
    public async Task List_is_paged_and_filtered_by_branch()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(AllPermissions);
        for (var i = 0; i < 3; i++)
            await client.PostJsonAsync(Route, NewWarehouse(branchId));

        var page = await (await client.GetAsync($"{Route}?branchId={branchId}&page=2&pageSize=2")).ReadAsync<JsonElement>();

        Assert.Equal((3, 2, 2, 2), (page.GetProperty("totalCount").GetInt32(), page.GetProperty("page").GetInt32(),
            page.GetProperty("pageSize").GetInt32(), page.GetProperty("totalPages").GetInt32()));
        Assert.Single(page.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Creating_needs_company_wide_access()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var response = await _api.CreateClient().As(AllPermissions, warehouses: Guid.NewGuid().ToString())
            .PostJsonAsync(Route, NewWarehouse(branchId));

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
    }

    [Fact]
    public async Task Update_changes_names_and_selling_keeps_code_and_type_and_a_stale_row_version_is_409()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(UpdatePermissions);
        var code = DatabaseFixture.Code("WH");
        var id = (await (await client.PostJsonAsync(Route, NewWarehouse(branchId, code))).ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        var rowVersion = (await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();

        var updated = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "مخزن الجملة", nameEn = (string?)null, allowsSelling = false, rowVersion });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var details = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal((code, "Physical", "مخزن الجملة", false), (details.GetProperty("code").GetString(), details.GetProperty("type").GetString(),
            details.GetProperty("nameAr").GetString(), details.GetProperty("allowsSelling").GetBoolean()));
        Assert.Equal((await updated.ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString(), details.GetProperty("rowVersion").GetString());

        await (await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "قديم", allowsSelling = true, rowVersion }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
        await (await client.PutJsonAsync($"{Route}/{Guid.NewGuid()}", new { nameAr = "مخزن", allowsSelling = true, rowVersion }))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Warehouse.NotFound");
    }

    [Fact]
    public async Task A_transit_warehouse_can_not_be_switched_to_selling()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(UpdatePermissions);
        var created = await client.PostJsonAsync(Route, NewWarehouse(branchId, type: WarehouseType.Transit, allowsSelling: false));
        var id = (await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        var rowVersion = (await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();

        var response = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "ترانزيت", allowsSelling = true, rowVersion });

        await response.AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Warehouse.TransitCannotSell");
    }

    [Fact]
    public async Task Changes_follow_the_warehouse_scope()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var admin = _api.CreateClient().As(UpdatePermissions);
        var mine = (await (await admin.PostJsonAsync(Route, NewWarehouse(branchId))).ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        var other = (await (await admin.PostJsonAsync(Route, NewWarehouse(branchId))).ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        var scoped = _api.CreateClient().As(UpdatePermissions, warehouses: mine.ToString());

        Assert.Equal(HttpStatusCode.NoContent, (await scoped.PostAsync($"{Route}/{mine}/deactivate", null)).StatusCode);
        await (await scoped.PostAsync($"{Route}/{other}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
        await (await scoped.PutJsonAsync($"{Route}/{other}", new { nameAr = "مخزن", allowsSelling = true, rowVersion = "AAAAAAAAB9E=" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Warehouse.AccessDenied");
    }

    [Fact]
    public async Task List_searches_and_filters_by_status()
    {
        var (branchId, _) = await fx.SeedBranchAndUnitAsync();
        var client = _api.CreateClient().As(UpdatePermissions);
        var active = (await (await client.PostJsonAsync(Route, NewWarehouse(branchId))).ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        var inactiveCode = DatabaseFixture.Code("WH");
        var inactive = (await (await client.PostJsonAsync(Route, NewWarehouse(branchId, inactiveCode))).ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsync($"{Route}/{inactive}/deactivate", null);

        var activeOnly = await (await client.GetAsync($"{Route}?branchId={branchId}&isActive=true")).ReadAsync<JsonElement>();
        Assert.Equal(active, Assert.Single(activeOnly.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var searched = await (await client.GetAsync($"{Route}?search={inactiveCode}")).ReadAsync<JsonElement>();
        Assert.Equal(inactive, Assert.Single(searched.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Page_size_above_the_maximum_is_400()
    {
        var response = await _api.CreateClient().As(AllPermissions).GetAsync($"{Route}?pageSize=101");
        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("pageSize", problem.GetProperty("errors")[0].GetProperty("property").GetString());
    }
}
