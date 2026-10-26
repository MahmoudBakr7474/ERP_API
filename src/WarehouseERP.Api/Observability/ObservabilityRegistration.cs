using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using WarehouseERP.Api.Security;
using WarehouseERP.Persistence;

namespace WarehouseERP.Api.Observability;

public static class ObservabilityRegistration
{
    public const string ReadyTag = "ready";

    // Serilog is the host's logging provider: levels and properties from the "Serilog" section,
    // readable text in Development and structured JSON everywhere else. Modules only see ILogger<T>.
    // Tracing and metrics (OpenTelemetry) can be added here later without touching modules.
    public static WebApplicationBuilder AddErpObservability(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();
        builder.Services.AddSerilog((services, logger) =>
        {
            logger.ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
            if (development)
                logger.WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}",
                    formatProvider: System.Globalization.CultureInfo.InvariantCulture);
            else
                logger.WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter());
        });

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ErpDbContext>("database", tags: [ReadyTag]);
        return builder;
    }

    public static IApplicationBuilder UseErpRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            // Path only: query strings can carry search text and are left out of logs.
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = (http, _, exception) =>
                exception is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Warning // the exception itself is logged once by GlobalExceptionHandler
                : http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, http) =>
            {
                if (http.User.FindFirst(ErpClaimTypes.Subject)?.Value is { } userId)
                    diagnostics.Set("UserId", userId);
            };
        });

    // Liveness: the process answers. Readiness: it can also reach SQL Server.
    public static IEndpointRouteBuilder MapErpHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains(ReadyTag) }).AllowAnonymous();
        return app;
    }
}
