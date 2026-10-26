using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;

// An administrator sets a new password; every session of the user ends.
public sealed partial class ResetUserPasswordHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IRoleRepository roles, ISessionRepository sessions,
    IEnumerable<PermissionDefinition> catalog, IIdentityPersistence persistence, TimeProvider clock,
    ILogger<ResetUserPasswordHandler> logger)
    : ICommandHandler<ResetUserPasswordCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(ResetUserPasswordCommand command, CancellationToken ct)
    {
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, accounts, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        var reset = await accounts.ResetPasswordAsync(command.UserId, command.NewPassword, ct);
        if (reset.IsFailure)
            return reset.Error;
        await SessionRevocation.RevokeAllAsync(sessions, command.UserId, SessionRevokeReason.PasswordReset, clock.GetUtcNow().UtcDateTime, ct);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PasswordReset(logger, command.UserId, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4012, Level = LogLevel.Information, Message = "Password of user {UserId} reset by {ActorId}; sessions revoked")]
    private static partial void PasswordReset(ILogger logger, Guid userId, Guid? actorId);
}
