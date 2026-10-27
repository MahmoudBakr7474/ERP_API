using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace WarehouseERP.Api.Security;

// Forwarded headers from the trusted proxies of ReverseProxyOptions, applied first in the pipeline. A wrong list
// stops the host at startup instead of silently trusting everybody (or nobody).
public static class ReverseProxySetup
{
    public static IServiceCollection AddErpReverseProxy(this IServiceCollection services)
    {
        services.AddOptions<ReverseProxyOptions>()
            .BindConfiguration(ReverseProxyOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(o => !o.Enabled || o.KnownProxies.Length + o.KnownNetworks.Length > 0,
                "ReverseProxy:Enabled needs at least one KnownProxies or KnownNetworks entry.")
            .Validate(o => o.KnownProxies.All(p => IPAddress.TryParse(p, out _)),
                "ReverseProxy:KnownProxies must hold IP addresses.")
            .Validate(o => o.KnownNetworks.All(n => IPNetwork.TryParse(n, out _)),
                "ReverseProxy:KnownNetworks must hold CIDR networks such as 10.0.0.0/24.")
            .ValidateOnStart();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ReverseProxyOptions>>((forwarded, proxy) =>
            {
                // None unless ReverseProxy is on: the host can add the forwarded-headers middleware by itself
                // (ASPNETCORE_FORWARDEDHEADERS_ENABLED), and with no known proxies that would trust every peer's headers.
                forwarded.ForwardedHeaders = proxy.Value.Enabled
                    ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    : ForwardedHeaders.None;
                forwarded.ForwardLimit = proxy.Value.ForwardLimit;
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();
                foreach (var address in proxy.Value.KnownProxies)
                    forwarded.KnownProxies.Add(IPAddress.Parse(address));
                foreach (var network in proxy.Value.KnownNetworks)
                    forwarded.KnownIPNetworks.Add(IPNetwork.Parse(network));
            });
        return services;
    }

    public static IApplicationBuilder UseErpForwardedHeaders(this IApplicationBuilder app)
    {
        if (app.ApplicationServices.GetRequiredService<IOptions<ReverseProxyOptions>>().Value.Enabled)
            app.UseForwardedHeaders();
        return app;
    }
}
