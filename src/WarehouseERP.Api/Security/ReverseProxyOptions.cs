using System.ComponentModel.DataAnnotations;

namespace WarehouseERP.Api.Security;

/// <summary>
/// Bound to <c>ReverseProxy</c>. Off by default. Turn it on only behind a TLS-terminating proxy, naming the proxies the
/// host trusts: the client IP (sign-in rate limit, security events) and the https scheme (sign-in refuses plain http)
/// are then read from their X-Forwarded-For / X-Forwarded-Proto headers. Headers from anyone else are ignored.
/// </summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; init; }

    /// <summary>Proxy addresses, e.g. <c>10.0.0.5</c>.</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Proxy networks in CIDR notation, e.g. <c>10.0.0.0/24</c>.</summary>
    public string[] KnownNetworks { get; init; } = [];

    /// <summary>How many proxy hops to unwind.</summary>
    [Range(1, 10)]
    public int ForwardLimit { get; init; } = 1;
}
