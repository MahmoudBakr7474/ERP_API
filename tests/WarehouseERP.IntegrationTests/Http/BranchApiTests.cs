using System.Net;
using System.Text.Json;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

[Collection(DatabaseCollection.Name)]
public sealed class BranchApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Route = "/api/organization/branches";
    private const string Warehouses = "/api/organization/warehouses";
    private static readonly string AllPermissions = string.Join(',', OrganizationPermissions.All);
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        await fx.EnsureCompanyAsync();
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions);

    private static async Task<Guid> IdOf(HttpResponseMessage created)
    {
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<(Guid Id, string Code)> CreateBranchAsync()
    {
        var code = DatabaseFixture.Code("BR");
        return (await IdOf(await Admin().PostJsonAsync(Route, new { code, nameAr = "فرع الاختبار", nameEn = "Test branch" })), code);
    }

    private async Task<Guid> CreateWarehouseAsync(Guid branchId) =>
        await IdOf(await Admin().PostJsonAsync(Warehouses,
            new { branchId, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = "Physical", allowsSelling = true }));

    [Fact]
    public async Task Create_returns_201_and_the_branch_reads_back_in_the_company()
    {
        var client = Admin();
        var created = await client.PostJsonAsync(Route, new { code = DatabaseFixture.Code("BR"), nameAr = " فرع ", nameEn = " " });
        var id = await IdOf(created);
        Assert.Equal($"{Route}/{id}", created.Headers.Location!.OriginalString);

        var branch = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();

        Assert.Equal(await fx.EnsureCompanyAsync(), branch.GetProperty("companyId").GetGuid());
        Assert.Equal(("فرع", JsonValueKind.Null, true),
            (branch.GetProperty("nameAr").GetString(), branch.GetProperty("nameEn").ValueKind, branch.GetProperty("isActive").GetBoolean()));
        Assert.False(string.IsNullOrEmpty(branch.GetProperty("rowVersion").GetString()));
    }

    [Fact]
    public async Task Duplicate_code_is_409_and_invalid_code_is_400()
    {
        var (_, code) = await CreateBranchAsync();

        await (await Admin().PostJsonAsync(Route, new { code, nameAr = "فرع" }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Branch.CodeTaken");
        var invalid = await (await Admin().PostJsonAsync(Route, new { code = "فرع 1", nameAr = "فرع" }))
            .AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        Assert.Equal("code", invalid.GetProperty("errors")[0].GetProperty("property").GetString());
    }

    [Fact]
    public async Task Changes_need_company_wide_access()
    {
        var (id, _) = await CreateBranchAsync();
        var scoped = _api.CreateClient().As(AllPermissions, warehouses: Guid.NewGuid().ToString());

        await (await scoped.PostJsonAsync(Route, new { code = DatabaseFixture.Code("BR"), nameAr = "فرع" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
        await (await scoped.PutJsonAsync($"{Route}/{id}", new { nameAr = "فرع", rowVersion = "AAAAAAAAB9E=" }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
        await (await scoped.PostAsync($"{Route}/{id}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
    }

    [Fact]
    public async Task Update_renames_keeps_the_code_and_a_stale_row_version_is_409()
    {
        var (id, code) = await CreateBranchAsync();
        var client = Admin();
        var rowVersion = (await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();

        var updated = await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "الفرع الرئيسي", nameEn = "Main", rowVersion });

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var branch = await (await client.GetAsync($"{Route}/{id}")).ReadAsync<JsonElement>();
        Assert.Equal((code, "الفرع الرئيسي", "Main"),
            (branch.GetProperty("code").GetString(), branch.GetProperty("nameAr").GetString(), branch.GetProperty("nameEn").GetString()));
        Assert.Equal((await updated.ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString(), branch.GetProperty("rowVersion").GetString());

        await (await client.PutJsonAsync($"{Route}/{id}", new { nameAr = "اسم قديم", rowVersion }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
        await (await client.PutJsonAsync($"{Route}/{Guid.NewGuid()}", new { nameAr = "فرع", rowVersion }))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Branch.NotFound");
    }

    [Fact]
    public async Task A_branch_with_active_warehouses_stays_active_and_an_inactive_branch_gets_no_warehouses()
    {
        var client = Admin();
        var (busy, _) = await CreateBranchAsync();
        var warehouse = await CreateWarehouseAsync(busy);
        await (await client.PostAsync($"{Route}/{busy}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Branch.HasActiveWarehouses");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Warehouses}/{warehouse}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Route}/{busy}/deactivate", null)).StatusCode);
        Assert.False((await (await client.GetAsync($"{Route}/{busy}")).ReadAsync<JsonElement>()).GetProperty("isActive").GetBoolean());

        await (await client.PostJsonAsync(Warehouses, new { branchId = busy, code = DatabaseFixture.Code("WH"), nameAr = "مخزن", type = "Physical", allowsSelling = true }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Branch.NotActive");
        await (await client.PostAsync($"{Warehouses}/{warehouse}/activate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Branch.NotActive");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Route}/{busy}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Warehouses}/{warehouse}/activate", null)).StatusCode);
        await (await client.PostAsync($"{Route}/{Guid.NewGuid()}/activate", null))
            .AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Branch.NotFound");
    }

    [Fact]
    public async Task List_searches_filters_by_status_and_pages()
    {
        var client = Admin();
        var tag = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
            await IdOf(await client.PostJsonAsync(Route, new { code = $"S{tag}-{i}", nameAr = "فرع" }));
        var (inactive, _) = await CreateBranchAsync();
        await client.PostAsync($"{Route}/{inactive}/deactivate", null);

        var page = await (await client.GetAsync($"{Route}?search={tag}&page=2&pageSize=2")).ReadAsync<JsonElement>();
        Assert.Equal((3, 2), (page.GetProperty("totalCount").GetInt32(), page.GetProperty("totalPages").GetInt32()));
        Assert.Equal($"S{tag}-2", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());

        var inactiveOnly = await (await client.GetAsync($"{Route}?isActive=false&pageSize=100")).ReadAsync<JsonElement>();
        Assert.Contains(inactiveOnly.GetProperty("items").EnumerateArray(), b => b.GetProperty("id").GetGuid() == inactive);
        Assert.All(inactiveOnly.GetProperty("items").EnumerateArray(), b => Assert.False(b.GetProperty("isActive").GetBoolean()));
    }

    [Fact]
    public async Task A_user_without_company_wide_access_reads_only_the_branches_of_their_warehouses()
    {
        var (mine, _) = await CreateBranchAsync();
        var (other, _) = await CreateBranchAsync();
        var warehouse = await CreateWarehouseAsync(mine);
        await CreateWarehouseAsync(other);
        var scoped = _api.CreateClient().As(OrganizationPermissions.BranchesRead, warehouses: warehouse.ToString());

        var list = await (await scoped.GetAsync($"{Route}?pageSize=100")).ReadAsync<JsonElement>();
        Assert.Equal(mine, Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, (await scoped.GetAsync($"{Route}/{mine}")).StatusCode);
        await (await scoped.GetAsync($"{Route}/{other}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Branch.AccessDenied");
        // Out of scope and missing look the same; a company-wide user learns it is missing.
        await (await scoped.GetAsync($"{Route}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Branch.AccessDenied");
        await (await Admin().GetAsync($"{Route}/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Branch.NotFound");
    }

    [Fact]
    public async Task Reading_needs_the_branches_permission()
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.WarehousesRead).GetAsync(Route);
        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }
}
