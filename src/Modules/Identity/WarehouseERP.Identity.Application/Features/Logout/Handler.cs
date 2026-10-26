using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Logout;

public sealed partial class LogoutHandler(
    ISessionRepository sessions, IIdentityPersistence persistence, TimeProvider clock, ILogger<LogoutHandler> logger)
    : ICommandHandler<LogoutCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(LogoutCommand command, CancellationToken ct)
    {
        if (await sessions.GetAsync(command.SessionId, ct) is not { } session)
            return Done.Value; // already gone: logging out is idempotent

        session.Revoke(SessionRevokeReason.Logout, clock.GetUtcNow().UtcDateTime);
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        SignedOut(logger, session.UserId, session.Id);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4006, Level = LogLevel.Information, Message = "User {UserId} signed out (session {SessionId})")]
    private static partial void SignedOut(ILogger logger, Guid userId, Guid sessionId);
}
