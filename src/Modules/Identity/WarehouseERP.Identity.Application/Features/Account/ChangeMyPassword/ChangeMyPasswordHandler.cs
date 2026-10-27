using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Account.ChangeMyPassword;

public sealed partial class ChangeMyPasswordHandler(
    ICurrentUser currentUser, IUserCredentials credentials, ISessionRepository sessions, IIdentityPersistence persistence,
    TimeProvider clock, ILogger<ChangeMyPasswordHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<ChangeMyPasswordCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(ChangeMyPasswordCommand command, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return UserErrors.NotFound;

        var (changed, lockoutStarted) = await credentials.ChangePasswordAsync(userId, command.CurrentPassword, command.NewPassword, ct);
        if (changed.IsFailure)
        {
            // A wrong current password counts toward lockout (written at once by IUserCredentials), and a locked-out
            // account can't change it at all, so a stolen session can't guess it unchecked.
            if (changed.Error == AuthenticationErrors.PasswordIncorrect || changed.Error == AuthenticationErrors.PasswordLockedOut)
            {
                securityEvents.Record(SecurityEventType.PasswordChangeRejected, userId, sessionId: command.CurrentSessionId,
                    details: new { reason = changed.Error.Code });
                if (lockoutStarted)
                    securityEvents.Record(SecurityEventType.UserLockedOut, userId, sessionId: command.CurrentSessionId,
                        details: new { channel = "password-change" });
                // Not the request's token: the attempt is counted already, so it is recorded even if the client left.
                _ = await persistence.SaveChangesAsync(CancellationToken.None);
            }
            return changed.Error;
        }
        await SessionRevocation.RevokeAllAsync(sessions, userId, SessionRevokeReason.PasswordChanged, clock.GetUtcNow().UtcDateTime, ct,
            keep: command.CurrentSessionId);
        securityEvents.Record(SecurityEventType.PasswordChanged, userId, sessionId: command.CurrentSessionId);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PasswordChanged(logger, userId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "User {UserId} changed their password; other sessions revoked")]
    private static partial void PasswordChanged(ILogger logger, Guid userId);
}
