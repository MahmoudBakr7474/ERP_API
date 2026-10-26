using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Account.ChangeMyPassword;

public sealed partial class ChangeMyPasswordHandler(
    ICurrentUser currentUser, IUserCredentials credentials, ISessionRepository sessions, IIdentityPersistence persistence,
    TimeProvider clock, ILogger<ChangeMyPasswordHandler> logger)
    : ICommandHandler<ChangeMyPasswordCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(ChangeMyPasswordCommand command, CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId)
            return UserErrors.NotFound;

        var changed = await credentials.ChangePasswordAsync(userId, command.CurrentPassword, command.NewPassword, ct);
        if (changed.IsFailure)
        {
            // A wrong current password counts toward lockout, so a stolen session can't guess it unchecked.
            if (changed.Error == AuthenticationErrors.PasswordIncorrect)
                _ = await persistence.SaveChangesAsync(ct);
            return changed.Error;
        }
        await SessionRevocation.RevokeAllAsync(sessions, userId, SessionRevokeReason.PasswordChanged, clock.GetUtcNow().UtcDateTime, ct,
            keep: command.CurrentSessionId);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PasswordChanged(logger, userId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "User {UserId} changed their password; other sessions revoked")]
    private static partial void PasswordChanged(ILogger logger, Guid userId);
}
