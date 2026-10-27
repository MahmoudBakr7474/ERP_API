using System.Net;
using System.Text.Json;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// The shared database's company (DatabaseFixture.EnsureCompanyAsync). First-run setup on an empty database is
// CompanySetupTests.
[Collection(DatabaseCollection.Name)]
public sealed class CompanyApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const string Route = "/api/organization/company";
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        await fx.EnsureCompanyAsync();
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private static object Settings(string rowVersion, string nameAr = "الشركة المحدثة", string timeZoneId = "Africa/Cairo") =>
        new { nameAr, nameEn = "Updated Co", timeZoneId, taxRegistrationNumber = "100-200-300", rowVersion };

    [Fact]
    public async Task Any_signed_in_user_reads_the_company()
    {
        var response = await _api.CreateClient().As(permissions: "", warehouses: Guid.NewGuid().ToString()).GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = await response.ReadAsync<JsonElement>();
        Assert.Equal(await fx.EnsureCompanyAsync(), company.GetProperty("id").GetGuid());
        Assert.Equal("EGP", company.GetProperty("baseCurrencyCode").GetString());
        Assert.False(string.IsNullOrEmpty(company.GetProperty("rowVersion").GetString()));
    }

    [Fact]
    public async Task Anonymous_read_is_401()
    {
        var response = await _api.CreateClient().GetAsync(Route);
        await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    [Fact]
    public async Task Update_changes_the_settings_and_a_stale_row_version_is_409()
    {
        var client = _api.CreateClient().As(OrganizationPermissions.CompanyUpdate);
        var before = await (await client.GetAsync(Route)).ReadAsync<JsonElement>();
        var rowVersion = before.GetProperty("rowVersion").GetString()!;

        var updated = await client.PutJsonAsync(Route, Settings(rowVersion));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var newRowVersion = (await updated.ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString();
        var after = await (await client.GetAsync(Route)).ReadAsync<JsonElement>();
        Assert.Equal(("الشركة المحدثة", "100-200-300", newRowVersion),
            (after.GetProperty("nameAr").GetString(), after.GetProperty("taxRegistrationNumber").GetString(), after.GetProperty("rowVersion").GetString()));
        Assert.Equal(before.GetProperty("code").GetString(), after.GetProperty("code").GetString());

        var stale = await client.PutJsonAsync(Route, Settings(rowVersion, nameAr: "اسم آخر"));
        await stale.AssertProblemAsync(HttpStatusCode.Conflict, "Common.ConcurrencyConflict");
    }

    [Fact]
    public async Task Update_by_a_user_without_company_wide_access_is_403()
    {
        var client = _api.CreateClient().As(OrganizationPermissions.CompanyUpdate, warehouses: Guid.NewGuid().ToString());
        var rowVersion = (await (await client.GetAsync(Route)).ReadAsync<JsonElement>()).GetProperty("rowVersion").GetString()!;

        var response = await client.PutJsonAsync(Route, Settings(rowVersion));

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
    }

    [Fact]
    public async Task Update_without_the_permission_is_403()
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.BranchesRead).PutJsonAsync(Route, Settings("AAAAAAAAB9E="));
        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Common.Forbidden");
    }

    [Fact]
    public async Task Unknown_time_zone_is_400()
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.CompanyUpdate)
            .PutJsonAsync(Route, Settings("AAAAAAAAB9E=", timeZoneId: "Mars/Olympus"));

        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var field = Assert.Single(problem.GetProperty("errors").EnumerateArray());
        Assert.Equal(("timeZoneId", "TimeZoneValidator"), (field.GetProperty("property").GetString(), field.GetProperty("code").GetString()));
    }

    [Fact]
    public async Task Setup_by_a_user_without_company_wide_access_is_403()
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.CompanyUpdate, warehouses: Guid.NewGuid().ToString())
            .PostJsonAsync(Route, new { code = DatabaseFixture.Code("C"), nameAr = "شركة", baseCurrencyCode = "EGP", timeZoneId = "Africa/Cairo" });

        await response.AssertProblemAsync(HttpStatusCode.Forbidden, "Organization.Company.CompanyWideAccessRequired");
    }

    [Fact]
    public async Task A_second_company_is_409()
    {
        var response = await _api.CreateClient().As(OrganizationPermissions.CompanyUpdate).PostJsonAsync(Route,
            new { code = DatabaseFixture.Code("C"), nameAr = "شركة ثانية", baseCurrencyCode = "USD", timeZoneId = "Africa/Cairo" });

        await response.AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Company.AlreadyConfigured");
    }
}
