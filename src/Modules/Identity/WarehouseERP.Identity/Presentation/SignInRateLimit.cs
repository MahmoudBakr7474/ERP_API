using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WarehouseERP.Identity.Presentation;

// Per-client limits on the credential endpoints, one fixed window per operation (SignInRateLimitOptions). The
// rejection itself (429 + Retry-After, ProblemDetails Common.TooManyRequests) belongs to the host, for every
// rate-limit policy.
internal static class SignInRateLimit
{
    // Named by the endpoints' OpenAPI operation ids, which are public API already.
    public const string Login = "Login";
    public const string IssueApiToken = "IssueApiToken";
    public const string RefreshApiToken = "RefreshApiToken";
    public const string ChangeMyPassword = "ChangeMyPassword";

    // Per minute, sized for a branch of about 30 users and 30 POS behind one address. A refresh token is a 256-bit
    // secret, so its limit only protects the server: POS devices refresh every few minutes, and in bursts after an
    // outage.
    private static readonly (string Operation, int PermitLimit)[] Defaults =
        [(Login, 60), (IssueApiToken, 60), (RefreshApiToken, 300), (ChangeMyPassword, 30)];

    public static IServiceCollection AddSignInRateLimit(this IServiceCollection services)
    {
        foreach (var (operation, permitLimit) in Defaults)
        {
            // Bound once, not with BindConfiguration: no reload, so the values validated at startup stay until the next
            // restart, like every other setting here, and a bad edit can't turn the operation into 500s at run time.
            services.AddOptions<SignInRateLimitOptions>(operation)
                .Configure<IConfiguration>((o, configuration) =>
                {
                    o.PermitLimit = permitLimit;
                    configuration.GetSection($"{SignInRateLimitOptions.SectionName}:{operation}").Bind(o);
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddRateLimiter(o => o.AddPolicy(PolicyName(operation), http =>
            {
                var limits = http.RequestServices.GetRequiredService<IOptionsMonitor<SignInRateLimitOptions>>().Get(operation);
                return RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(http.Connection.RemoteIpAddress),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limits.PermitLimit, Window = limits.Window, QueueLimit = 0 });
            }));
        }
        return services;
    }

    public static RouteHandlerBuilder WithSignInRateLimit(this RouteHandlerBuilder builder, string operation) =>
        builder.RequireRateLimiting(PolicyName(operation)).ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static string PolicyName(string operation) => "identity-" + operation;

    // One budget per client address. An IPv4-mapped IPv6 address is the IPv4 address it maps. An IPv6 address counts
    // as its /64: a subscriber gets at least a whole /64, so per-address budgets could be renewed at will. That makes
    // one IPv6 LAN segment (and every link-local client) one budget, like a NAT address, while a client holding a
    // larger prefix still gets one budget per /64. Without an address (only the test host and socket transports have
    // none) every request shares one budget.
    private static string ClientKey(IPAddress? address)
    {
        if (address is null)
            return "unknown";
        if (address.IsIPv4MappedToIPv6)
            return address.MapToIPv4().ToString();
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return address.ToString();
        var prefix = address.GetAddressBytes();
        Array.Clear(prefix, 8, 8);
        return new IPAddress(prefix) + "/64";
    }
}
