using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Application.Messaging;

// Logs every command's outcome with its name, duration and error code. Never the payload: commands
// can carry personal or commercial data. Unexpected exceptions are not caught here; the host's
// exception handler logs them once.
public sealed class LoggingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner, ILogger<LoggingCommandHandler<TCommand, TResponse>> logger)
    : ICommandHandler<TCommand, TResponse>
{
    private static readonly string CommandName = typeof(TCommand).Name;

    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var result = await inner.HandleAsync(command, ct);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (result.IsSuccess)
            HandlerLog.CommandSucceeded(logger, CommandName, elapsedMs);
        else if (result.Error.Type == ErrorType.Validation)
            HandlerLog.CommandRejected(logger, CommandName, result.Error.Code, elapsedMs);
        else
            HandlerLog.CommandFailed(logger, CommandName, result.Error.Code, result.Error.Type, elapsedMs);

        return result;
    }
}

internal static partial class HandlerLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "{Command} succeeded in {ElapsedMs:0.0} ms")]
    public static partial void CommandSucceeded(ILogger logger, string command, double elapsedMs);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "{Command} rejected by validation ({ErrorCode}) in {ElapsedMs:0.0} ms")]
    public static partial void CommandRejected(ILogger logger, string command, string errorCode, double elapsedMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "{Command} failed with {ErrorCode} ({ErrorType}) in {ElapsedMs:0.0} ms")]
    public static partial void CommandFailed(ILogger logger, string command, string errorCode, ErrorType errorType, double elapsedMs);
}
