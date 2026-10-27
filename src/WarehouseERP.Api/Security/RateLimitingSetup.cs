using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace WarehouseERP.Api.Security;

// The rejection side of every rate-limit policy (modules declare their own policies, e.g. Identity's sign-in limit):
// 429 with Retry-After, and the ProblemDetails body (Common.TooManyRequests) written by the status-code pages.
public static partial class RateLimitingSetup
{
    public static IServiceCollection AddErpRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (context, _) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    http.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                Refused(
                    http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RateLimitingSetup)),
                    http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName,
                    http.Request.Method, http.Request.Path.Value, http.Connection.RemoteIpAddress?.ToString());
                return ValueTask.CompletedTask;
            };
        });
        return services;
    }

    [LoggerMessage(EventId = 9010, Level = LogLevel.Warning,
        Message = "Rate limit {RateLimitPolicy} refused {RequestMethod} {RequestPath} from {ClientIp}")]
    private static partial void Refused(ILogger logger, string? rateLimitPolicy, string requestMethod, string? requestPath, string? clientIp);
}
