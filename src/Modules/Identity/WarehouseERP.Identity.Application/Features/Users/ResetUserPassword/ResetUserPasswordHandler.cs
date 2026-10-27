using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;

// An administrator sets a new password; every session of the user ends.
public sealed partial class ResetUserPasswordHandler(
    ICurrentUser currentUser, IUserCredentials credentials, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access, ISessionRepository sessions,
    IEnumerable<PermissionDefinition> catalog, IIdentityPersistence persistence, TimeProvider clock,
    ILogger<ResetUserPasswordHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<ResetUserPasswordCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(ResetUserPasswordCommand command, CancellationToken ct)
    {
        // A reset skips the current password and the lockout count: for oneself that would turn a stolen session into
        // a permanent takeover in one call.
        if (command.UserId == currentUser.UserId)
            return UserErrors.CannotResetOwnPassword;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, access, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return await SecurityAudit.RefuseAsync(allowed.Error, securityEvents, persistence, ct, command.UserId);
        var reset = await credentials.ResetPasswordAsync(command.UserId, command.NewPassword, ct);
        if (reset.IsFailure)
            return reset.Error;
        await SessionRevocation.RevokeAllAsync(sessions, command.UserId, SessionRevokeReason.PasswordReset, clock.GetUtcNow().UtcDateTime, ct);
        securityEvents.Record(SecurityEventType.PasswordReset, command.UserId);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PasswordReset(logger, command.UserId, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4012, Level = LogLevel.Information, Message = "Password of user {UserId} reset by {ActorId}; sessions revoked")]
    private static partial void PasswordReset(ILogger logger, Guid userId, Guid? actorId);
}
