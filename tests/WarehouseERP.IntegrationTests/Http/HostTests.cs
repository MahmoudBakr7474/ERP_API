using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// The host itself: health, unexpected failures, malformed requests, options validation, DI, OpenAPI.
[Collection(DatabaseCollection.Name)]
public sealed class HostTests(DatabaseFixture fx)
{
    // A LocalDB instance that does not exist fails fast, standing in for an unreachable database.
    private const string BrokenDatabase = @"Server=(localdb)\WarehouseERP_NoSuchInstance;Database=x;Trusted_Connection=True;Connect Timeout=3";

    [Fact]
    public async Task Liveness_and_readiness_are_healthy_with_a_database()
    {
        await using var api = new ApiFactory(fx.ConnectionString);
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task Without_a_database_the_process_is_live_but_not_ready()
    {
        await using var api = new ApiFactory(BrokenDatabase);
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Unexpected_failure_is_a_sanitized_500()
    {
        await using var api = new ApiFactory(BrokenDatabase);
        var response = await api.CreateClient().As(CatalogPermissions.ProductsRead)
            .GetAsync("/api/catalog/products");

        var problem = await response.AssertProblemAsync(HttpStatusCode.InternalServerError, "Common.Unexpected");
        var body = problem.GetRawText();
        Assert.DoesNotContain("SqlException", body, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("NoSuchInstance", body, StringComparison.Ordinal);
        Assert.False(problem.TryGetProperty("exception", out _));
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Unexpected_failure_is_logged_once_with_its_exception_and_correlation_id()
    {
        var sink = new CollectingSink();
        await using var api = new ApiFactory(BrokenDatabase, logSink: sink);
        var client = api.CreateClient().As(CatalogPermissions.ProductsRead);
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "fail-once");

        await client.GetAsync("/api/catalog/products");

        // Only this request's events: with the database down, Data Protection also logs (once, at startup) that it
        // can't read its key ring; that is a separate failure, not a second log of this one.
        var request = sink.Events
            .Where(e => e.Properties.TryGetValue("CorrelationId", out var id) && id.ToString() == "\"fail-once\"")
            .ToList();
        var logged = Assert.Single(request, e => e.Exception is not null);
        Assert.Equal(Serilog.Events.LogEventLevel.Error, logged.Level);
        Assert.DoesNotContain(request, e => e.Level >= Serilog.Events.LogEventLevel.Error && e != logged);
    }

    [Fact]
    public async Task Malformed_json_is_400_not_500()
    {
        await using var api = new ApiFactory(fx.ConnectionString);
        var content = new StringContent("{ \"sku\": ", System.Text.Encoding.UTF8, "application/json");
        var response = await api.CreateClient().As(CatalogPermissions.ProductsCreate).PostAsync("/api/catalog/products", content);

        await response.AssertProblemAsync(HttpStatusCode.BadRequest, "Common.BadRequest");
    }

    [Fact]
    public async Task Correlation_id_from_the_caller_comes_back_in_header_and_problem()
    {
        await using var api = new ApiFactory(fx.ConnectionString);
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "order-42");

        var response = await client.GetAsync("/api/organization/warehouses");

        Assert.Equal("order-42", response.Headers.GetValues("X-Correlation-ID").Single());
        var problem = await response.AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
        Assert.Equal("order-42", problem.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData("Database:ConnectionString", "")]
    [InlineData("Database:CommandTimeoutSeconds", "0")]
    [InlineData("Pagination:MaxPageSize", "0")]
    [InlineData("Pagination:DefaultPageSize", "200")]
    [InlineData("Identity:Sessions:WebIdleTimeout", "2.00:00:00")]
    [InlineData("Identity:Sessions:ApiAccessTokenLifetime", "30.00:00:00")]
    [InlineData("Identity:Password:RequiredLength", "4")]
    [InlineData("Identity:Lockout:MaxFailedAccessAttempts", "0")]
    [InlineData("Identity:Bootstrap:UserName", "admin")] // without a password
    [InlineData("Identity:Sessions:RetentionAfterEnd", "00:00:01")]
    [InlineData("Identity:SignInRateLimit:Login:PermitLimit", "0")]
    [InlineData("Identity:SignInRateLimit:RefreshApiToken:Window", "00:00:00")]
    [InlineData("ReverseProxy:Enabled", "true")] // without a trusted proxy
    [InlineData("ReverseProxy:KnownProxies:0", "not-an-address")]
    [InlineData("ReverseProxy:KnownNetworks:0", "10.0.0.0")] // not CIDR
    public void Invalid_required_settings_stop_the_host_at_startup(string key, string value)
    {
        var settings = new Dictionary<string, string?> { [key] = value };
        var connection = key == "Database:ConnectionString" ? value : fx.ConnectionString;

        using var api = new ApiFactory(connection, settings: settings);
        Assert.Throws<OptionsValidationException>(() => api.CreateClient());
    }

    [Fact]
    public async Task Identity_sessions_are_the_only_authentication_scheme()
    {
        await using var api = new ApiFactory(fx.ConnectionString, realSessions: true);
        var schemes = api.Services.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

        var scheme = Assert.Single(await schemes.GetAllSchemesAsync());
        Assert.Equal("ErpSession", scheme.Name);
        Assert.Equal("ErpSession", (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
    }

    [Fact]
    public async Task Development_host_validates_the_whole_container_and_serves_openapi()
    {
        // In Development the host builds with ValidateOnBuild: any unresolvable service fails here.
        await using var api = new ApiFactory(fx.ConnectionString, environment: "Development");
        var document = await (await api.CreateClient().GetAsync("/openapi/v1.json")).Content.ReadAsStringAsync();
        var paths = JsonDocument.Parse(document).RootElement.GetProperty("paths");

        foreach (var path in new[]
                 {
                     "/api/organization/warehouses", "/api/organization/warehouses/{id}", "/api/catalog/products", "/api/catalog/products/{id}",
                     "/api/identity/login", "/api/identity/tokens", "/api/identity/me", "/api/identity/users", "/api/identity/roles",
                 })
            Assert.True(paths.TryGetProperty(path, out _), $"OpenAPI has no {path}");
    }

    // The React client is generated from this document: any change to a path, schema name, property, enum member or
    // status code shows up here first. After a deliberate change, regenerate the committed copy with
    // ERP_UPDATE_OPENAPI_SNAPSHOT=1 and review its diff like code.
    [Fact]
    public async Task OpenApi_document_matches_the_committed_contract()
    {
        await using var api = new ApiFactory(fx.ConnectionString, environment: "Development");
        var response = await api.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var served = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        served.Remove("servers"); // the test server's address, not part of the contract
        var snapshot = Path.Combine(RepositoryRoot(), "docs", "api", "openapi-v1.json");

        if (Environment.GetEnvironmentVariable("ERP_UPDATE_OPENAPI_SNAPSHOT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshot)!);
            await File.WriteAllTextAsync(snapshot, served.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
            // Never green in update mode: a variable left set would otherwise accept every later contract change.
            Assert.Fail("docs/api/openapi-v1.json was regenerated. Review its diff, then run again without ERP_UPDATE_OPENAPI_SNAPSHOT.");
        }

        Assert.True(File.Exists(snapshot), $"No committed OpenAPI contract at {snapshot}; run with ERP_UPDATE_OPENAPI_SNAPSHOT=1.");
        var committed = JsonNode.Parse(await File.ReadAllTextAsync(snapshot));
        Assert.True(JsonNode.DeepEquals(committed, served),
            "The OpenAPI document differs from docs/api/openapi-v1.json. If the change is intended, regenerate it with ERP_UPDATE_OPENAPI_SNAPSHOT=1.");
    }

    [Fact]
    public async Task Production_host_does_not_serve_openapi()
    {
        await using var api = new ApiFactory(fx.ConnectionString);
        var response = await api.CreateClient().As("x").GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "WarehouseERP.slnx")))
                return dir.FullName;
        throw new InvalidOperationException("WarehouseERP.slnx not found above " + AppContext.BaseDirectory);
    }
}
