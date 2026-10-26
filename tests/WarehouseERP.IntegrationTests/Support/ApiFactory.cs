using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;
using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.IntegrationTests;

// The real API host on a test database. By default authentication is the test scheme below: the
// request's X-Test-* headers say who the user is, so module tests exercise the real authorization
// policies and warehouse checks without signing in. With realSessions the host keeps Identity's
// session authentication (cookies, bearer tokens) and clients talk https so Secure cookies flow.
public sealed class ApiFactory(string connectionString, string environment = "Production",
    IReadOnlyDictionary<string, string?>? settings = null, TimeProvider? clock = null, ILogEventSink? logSink = null,
    bool realSessions = false)
    : WebApplicationFactory<Program>
{
    /// <summary>A client over https; with <paramref name="handleCookies"/> it keeps its own cookie jar, like a browser.</summary>
    public HttpClient CreateBrowser(bool handleCookies = true) =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = handleCookies });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var all = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = connectionString,
            ["Serilog:MinimumLevel:Default"] = "Warning",
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            all[key] = value;

        builder.UseEnvironment(environment);
        // UseSetting: visible to Program while it builds.
        // In-memory source: added last, so it also wins over appsettings*.json when options bind.
        foreach (var (key, value) in all)
            builder.UseSetting(key, value);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(all));

        builder.ConfigureTestServices(services =>
        {
            if (!realSessions)
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultForbidScheme = TestAuthHandler.SchemeName;
                });
            }
            if (clock is not null)
                services.AddSingleton(clock);
            if (logSink is not null)
                services.AddSingleton(logSink); // picked up by Serilog's ReadFrom.Services
        });
    }
}

/// <summary>Collects every Serilog event the host writes.</summary>
public sealed class CollectingSink : ILogEventSink
{
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events
    {
        get { lock (_events) return [.. _events]; }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_events) _events.Add(logEvent);
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";
    public const string PermissionsHeader = "X-Test-Permissions";
    public const string WarehousesHeader = "X-Test-Warehouses";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers[UserHeader].ToString();
        if (user.Length == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ErpClaimTypes.Subject, user) };
        claims.AddRange(Split(PermissionsHeader).Select(p => new Claim(ErpClaimTypes.Permission, p)));
        claims.AddRange(Split(WarehousesHeader).Select(w => new Claim(ErpClaimTypes.Warehouse, w)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    private string[] Split(string header) =>
        Request.Headers[header].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
