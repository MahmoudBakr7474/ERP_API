using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Api.ErrorHandling;

// The last line for failures nobody expected. Each exception is logged once, here; the response
// is a ProblemDetails with a stable errorCode and never a stack trace or database detail.
// Expected failures never get here: handlers return Result and endpoints map it.
internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public const string UnexpectedCode = "Common.Unexpected";
    public const string BadRequestCode = "Common.BadRequest";

    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        int status;
        string code, detail;
        switch (exception)
        {
            case BadHttpRequestException bad: // malformed JSON, missing body, bad route value
                (status, code, detail) = (bad.StatusCode, BadRequestCode, "The request could not be read.");
                RequestUnreadable(logger, bad.Message);
                break;
            case DbUpdateConcurrencyException: // a save outside the repositories' save points
                (status, code, detail) = (StatusCodes.Status409Conflict, CommonErrors.ConcurrencyConflict.Code, CommonErrors.ConcurrencyConflict.Message);
                ConcurrencyConflict(logger, exception);
                break;
            default:
                (status, code, detail) = (StatusCodes.Status500InternalServerError, UnexpectedCode, "An unexpected error occurred.");
                Unexpected(logger, exception);
                break;
        }

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Detail = detail,
                Extensions = { [ResultHttpMapping.ErrorCodeKey] = code },
            },
        });
    }

    [LoggerMessage(EventId = 9001, Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void Unexpected(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9002, Level = LogLevel.Warning, Message = "Unhandled concurrency conflict")]
    private static partial void ConcurrencyConflict(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9003, Level = LogLevel.Information, Message = "Unreadable request: {Reason}")]
    private static partial void RequestUnreadable(ILogger logger, string reason);
}
