using System.Diagnostics;
using Serilog.Context;

namespace WarehouseERP.Api.Observability;

// One id per request across logs, error responses and the response header. A caller's
// X-Correlation-ID is kept when it is short and plain; otherwise the trace id is used.
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext http)
    {
        var given = http.Request.Headers[HeaderName].ToString();
        var correlationId = IsSafe(given) ? given : Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;

        http.Items[ItemKey] = correlationId;
        // Written as the response starts: the exception handler clears headers set earlier.
        http.Response.OnStarting(() =>
        {
            http.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        using (LogContext.PushProperty(ItemKey, correlationId))
            await next(http);
    }

    // Never echo arbitrary input into headers and logs.
    private static bool IsSafe(string value) =>
        value.Length is > 0 and <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
