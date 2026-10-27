using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.IntegrationTests.Http;

// First run needs a database without a company, so this class migrates its own instead of sharing the
// collection's one (which always has its company). One test, so one extra database.
public sealed class CompanySetupTests : IAsyncLifetime
{
    private const string Route = "/api/organization/company";
    private readonly DatabaseFixture _db = new();
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _api = new ApiFactory(_db.ConnectionString, clock: _db.Clock);
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _db.DisposeAsync();
    }

    private static object NewCompany(string code) =>
        new { code, nameAr = "شركة التوزيع", nameEn = "Distribution Co", baseCurrencyCode = "egp", timeZoneId = "Africa/Cairo", taxRegistrationNumber = (string?)null };

    [Fact]
    public async Task First_run_sets_up_one_company_and_concurrent_setups_cannot_both_succeed()
    {
        var client = _api.CreateClient().As(OrganizationPermissions.CompanyUpdate);
        await (await client.GetAsync(Route)).AssertProblemAsync(HttpStatusCode.NotFound, "Organization.Company.NotConfigured");
        await (await _api.CreateClient().As(OrganizationPermissions.BranchesCreate).PostJsonAsync("/api/organization/branches",
                new { code = "B-1", nameAr = "فرع" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Organization.Company.SetupRequired");

        var responses = await Task.WhenAll(
            client.PostJsonAsync(Route, NewCompany("CO-1")),
            _api.CreateClient().As(OrganizationPermissions.CompanyUpdate).PostJsonAsync(Route, NewCompany("CO-2")));

        var created = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        var refused = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.Created);
        await refused.AssertProblemAsync(HttpStatusCode.Conflict, "Organization.Company.AlreadyConfigured");
        Assert.Equal(Route, created.Headers.Location!.OriginalString);

        var company = await (await client.GetAsync(Route)).ReadAsync<JsonElement>();
        Assert.Equal((await created.ReadAsync<JsonElement>()).GetProperty("id").GetGuid(), company.GetProperty("id").GetGuid());
        Assert.Equal(("EGP", "Africa/Cairo"), (company.GetProperty("baseCurrencyCode").GetString(), company.GetProperty("timeZoneId").GetString()));
        await using var db = _db.NewDb();
        Assert.Equal(1, await db.Set<Company>().CountAsync());
    }
}
