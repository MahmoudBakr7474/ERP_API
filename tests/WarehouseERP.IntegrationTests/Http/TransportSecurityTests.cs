using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// HTTPS and the client address end to end. Tokens count over HTTPS only. Forwarded headers count only from the
// proxies the ReverseProxy settings trust, and then they also decide the rate-limit budget and the audited address.
[Collection(DatabaseCollection.Name)]
public sealed class TransportSecurityTests(DatabaseFixture fx)
{
    private const string TrustedProxy = "10.0.0.5";

    [Fact]
    public async Task A_bearer_token_counts_over_https_only_and_token_responses_are_never_cached()
    {
        await using var api = new ApiFactory(fx.ConnectionString, realSessions: true);
        var account = await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        var issued = await api.CreateBrowser(handleCookies: false)
            .PostJsonAsync("/api/identity/tokens", new { userName = account.UserName, password = account.Password });

        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        Assert.True(issued.Headers.CacheControl?.NoStore);
        var token = (await issued.ReadAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var overHttps = await api.CreateBrowser(handleCookies: false).SendAsBearerAsync(HttpMethod.Get, "/api/identity/me", token);
        Assert.Equal(HttpStatusCode.OK, overHttps.StatusCode);
        await (await api.CreateClient().SendAsBearerAsync(HttpMethod.Get, "/api/identity/me", token)) // http://localhost
            .AssertProblemAsync(HttpStatusCode.Unauthorized, "Common.Unauthorized");
    }

    [Fact]
    public async Task Behind_the_trusted_proxy_the_forwarded_scheme_and_client_count()
    {
        await using var api = BehindProxy(connectingFrom: TrustedProxy);
        var account = await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        var login = await Forwarded(api, "203.0.113.7")
            .PostJsonAsync("/api/identity/login", new { userName = account.UserName, password = account.Password });

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode); // plain http to the host, https at the proxy
        await using var db = fx.NewDb();
        Assert.Equal("203.0.113.7", await db.Set<SecurityEvent>()
            .Where(e => e.TargetUserId == account.Id && e.Type == SecurityEventType.SignedIn)
            .Select(e => e.IpAddress).SingleAsync());
    }

    [Fact]
    public async Task Forwarded_headers_from_any_other_peer_are_ignored()
    {
        await using var api = BehindProxy(connectingFrom: "192.0.2.10");
        var account = await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        await (await Forwarded(api, "203.0.113.7")
                .PostJsonAsync("/api/identity/login", new { userName = account.UserName, password = account.Password }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.HttpsRequired");
    }

    // ASPNETCORE_FORWARDEDHEADERS_ENABLED makes the host add the forwarded-headers middleware with no known proxies.
    [Fact]
    public async Task The_hosts_own_forwarded_headers_switch_trusts_nobody_without_ReverseProxy()
    {
        await using var api = new ApiFactory(fx.ConnectionString, realSessions: true,
            settings: new Dictionary<string, string?> { ["ForwardedHeaders_Enabled"] = "true" });
        var account = await api.SeedUserAsync([OrganizationPermissions.WarehousesRead]);

        await (await Forwarded(api, "203.0.113.7")
                .PostJsonAsync("/api/identity/login", new { userName = account.UserName, password = account.Password }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Identity.Session.HttpsRequired");
    }

    [Fact]
    public async Task Each_client_behind_the_proxy_has_its_own_budget_and_an_IPv6_client_is_its_64()
    {
        await using var api = BehindProxy(connectingFrom: TrustedProxy, ("Identity:SignInRateLimit:Login:PermitLimit", "1"));
        var account = await api.SeedUserAsync();
        Task<HttpResponseMessage> LoginFrom(string client) =>
            Forwarded(api, client).PostJsonAsync("/api/identity/login", new { userName = account.UserName, password = "Wrong-password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFrom("203.0.113.7")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFrom("198.51.100.9")).StatusCode); // another branch
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginFrom("2001:db8:1:2::1")).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginFrom("::ffff:203.0.113.7")).StatusCode); // the same IPv4 client
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginFrom("2001:db8:1:2::99")).StatusCode); // the same /64
    }

    // The host behind a TLS-terminating proxy at 10.0.0.5. The test server has no connection address, and the
    // forwarded-headers middleware trusts a missing one, so each test sets the address its requests come from.
    private ApiFactory BehindProxy(string connectingFrom, params (string Key, string Value)[] settings)
    {
        var all = new Dictionary<string, string?> { ["ReverseProxy:Enabled"] = "true", ["ReverseProxy:KnownProxies:0"] = TrustedProxy };
        foreach (var (key, value) in settings)
            all[key] = value;
        return new ApiFactory(fx.ConnectionString, realSessions: true, settings: all,
            testServices: s => s.AddSingleton<IStartupFilter>(new ConnectionFrom(IPAddress.Parse(connectingFrom))));
    }

    // Plain http to the host, as a proxy that terminated TLS for this client would send it.
    private static HttpClient Forwarded(ApiFactory api, string client)
    {
        var http = api.CreateClient(); // http://localhost
        http.DefaultRequestHeaders.Add("X-Forwarded-For", client);
        http.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        return http;
    }

    private sealed class ConnectionFrom(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
