using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Sessions.RevokeUserSessions;

// Signs a user out everywhere (suspected compromise). Endpoint permission: identity.sessions.manage.
public sealed partial class RevokeUserSessionsHandler(
    ICurrentUser currentUser, IUserAccounts accounts, ISessionRepository sessions, IIdentityPersistence persistence,
    TimeProvider clock, ILogger<RevokeUserSessionsHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<RevokeUserSessionsCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(RevokeUserSessionsCommand command, CancellationToken ct)
    {
        if (await accounts.FindAsync(command.UserId, ct) is null)
            return UserErrors.NotFound;
        await SessionRevocation.RevokeAllAsync(sessions, command.UserId, SessionRevokeReason.RevokedByAdministrator, clock.GetUtcNow().UtcDateTime, ct);
        securityEvents.Record(SecurityEventType.UserSessionsRevoked, command.UserId);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AllRevoked(logger, command.UserId, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4031, Level = LogLevel.Information, Message = "All sessions of user {UserId} revoked by {ActorId}")]
    private static partial void AllRevoked(ILogger logger, Guid userId, Guid? actorId);
}
