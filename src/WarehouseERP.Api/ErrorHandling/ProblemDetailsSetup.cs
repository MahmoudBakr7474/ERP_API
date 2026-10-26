using System.Diagnostics;
using WarehouseERP.Api.Observability;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Api.ErrorHandling;

public static class ProblemDetailsSetup
{
    // Every error response, from a Result, an exception or the framework (401/403/404/405), carries
    // the same fields so the React client handles one shape: errorCode, traceId, correlationId.
    public static IServiceCollection AddErpProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var http = context.HttpContext;
            var problem = context.ProblemDetails;
            problem.Instance ??= http.Request.Path;
            problem.Extensions.TryAdd(ResultHttpMapping.ErrorCodeKey, FrameworkErrorCode(problem.Status));
            problem.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
            if (http.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
                problem.Extensions["correlationId"] = correlationId;
        });
        return services;
    }

    private static string FrameworkErrorCode(int? status) => status switch
    {
        StatusCodes.Status400BadRequest => GlobalExceptionHandler.BadRequestCode,
        StatusCodes.Status401Unauthorized => "Common.Unauthorized",
        StatusCodes.Status403Forbidden => "Common.Forbidden",
        StatusCodes.Status404NotFound => "Common.NotFound",
        StatusCodes.Status405MethodNotAllowed => "Common.MethodNotAllowed",
        StatusCodes.Status415UnsupportedMediaType => "Common.UnsupportedMediaType",
        _ => GlobalExceptionHandler.UnexpectedCode,
    };
}
