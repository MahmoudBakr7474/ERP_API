using System.Text.Json;
using Microsoft.AspNetCore.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Http;

/// <summary>A field failure as clients see it: the property in camelCase, matching the JSON body.</summary>
public sealed record ProblemFieldError(string Property, string Code, string Message);

// The one place an expected failure becomes HTTP: ProblemDetails (RFC 9457) with a stable
// "errorCode" extension, plus "errors" for validation. The host adds traceId/correlationId.
public static class ResultHttpMapping
{
    public const string ErrorCodeKey = "errorCode";
    public const string ErrorsKey = "errors";

    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unmapped error type."),
    };

    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { [ErrorCodeKey] = error.Code };
        if (error.ValidationErrors.Count > 0)
            extensions[ErrorsKey] = error.ValidationErrors
                .Select(e => new ProblemFieldError(CamelCasePath(e.Property), e.Code, e.Message))
                .ToList();
        return TypedResults.Problem(detail: error.Message, statusCode: StatusCodeFor(error.Type), extensions: extensions);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    // "Lines[0].UnitId" -> "lines[0].unitId"
    private static string CamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
