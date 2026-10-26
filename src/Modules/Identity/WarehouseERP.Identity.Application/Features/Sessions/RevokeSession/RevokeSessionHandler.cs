using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Sessions.RevokeSession;

// Ends one session (a forgotten browser, a lost POS) on its next request. Anyone may end their own; another user's
// needs identity.sessions.manage, and to everyone else it doesn't exist.
public sealed partial class RevokeSessionHandler(
    ICurrentUser currentUser, ISessionRepository sessions, IIdentityPersistence persistence, TimeProvider clock,
    ILogger<RevokeSessionHandler> logger)
    : ICommandHandler<RevokeSessionCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(RevokeSessionCommand command, CancellationToken ct)
    {
        var session = await sessions.GetAsync(command.SessionId, ct);
        var own = session is not null && session.UserId == currentUser.UserId;
        if (session is null || (!own && !currentUser.HasPermission(IdentityPermissions.SessionsManage)))
            return SessionErrors.NotFound;

        session.Revoke(own ? SessionRevokeReason.RevokedByUser : SessionRevokeReason.RevokedByAdministrator, clock.GetUtcNow().UtcDateTime);
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        SessionRevoked(logger, session.Id, session.UserId, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4030, Level = LogLevel.Information, Message = "Session {SessionId} of user {UserId} revoked by {ActorId}")]
    private static partial void SessionRevoked(ILogger logger, Guid sessionId, Guid userId, Guid? actorId);
}
