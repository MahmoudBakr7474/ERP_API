using System.Net;
using System.Text.Json;
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
    [InlineData("Authentication:Jwt:Authority", "http://issuer.example.com")]
    public void Invalid_required_settings_stop_the_host_at_startup(string key, string value)
    {
        var settings = new Dictionary<string, string?> { [key] = value };
        if (key.StartsWith("Authentication:Jwt", StringComparison.Ordinal))
            settings["Authentication:Jwt:Audience"] = "warehouse-erp";
        var connection = key == "Database:ConnectionString" ? value : fx.ConnectionString;

        using var api = new ApiFactory(connection, settings: settings);
        Assert.Throws<OptionsValidationException>(() => api.CreateClient());
    }

    [Fact]
    public async Task Trusted_issuer_switches_authentication_to_jwt_bearer()
    {
        await using var api = new ApiFactory(fx.ConnectionString, settings: new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Authority"] = "https://issuer.example.com",
            ["Authentication:Jwt:Audience"] = "warehouse-erp",
        });
        var schemes = api.Services.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

        Assert.NotNull(await schemes.GetSchemeAsync("Bearer"));
        Assert.Null(await schemes.GetSchemeAsync(Api.Security.SecurityRegistration.NoIdentityProviderScheme));
    }

    [Fact]
    public async Task Development_host_validates_the_whole_container_and_serves_openapi()
    {
        // In Development the host builds with ValidateOnBuild: any unresolvable service fails here.
        await using var api = new ApiFactory(fx.ConnectionString, environment: "Development");
        var document = await (await api.CreateClient().GetAsync("/openapi/v1.json")).Content.ReadAsStringAsync();
        var paths = JsonDocument.Parse(document).RootElement.GetProperty("paths");

        foreach (var path in new[] { "/api/organization/warehouses", "/api/organization/warehouses/{id}", "/api/catalog/products", "/api/catalog/products/{id}" })
            Assert.True(paths.TryGetProperty(path, out _), $"OpenAPI has no {path}");
    }

    [Fact]
    public async Task Production_host_does_not_serve_openapi()
    {
        await using var api = new ApiFactory(fx.ConnectionString);
        var response = await api.CreateClient().As("x").GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
