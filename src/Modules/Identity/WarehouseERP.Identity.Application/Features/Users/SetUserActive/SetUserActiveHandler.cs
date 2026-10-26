using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Sessions;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.SetUserActive;

// Deactivation ends every session at once; the last active Administrator and the caller themselves can't be
// deactivated.
public sealed partial class SetUserActiveHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IUserRoleAssignments roleAssignments, IRoleRepository roles, ISessionRepository sessions,
    IEnumerable<PermissionDefinition> catalog, IIdentityPersistence persistence, TimeProvider clock,
    ILogger<SetUserActiveHandler> logger)
    : ICommandHandler<SetUserActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUserActiveCommand command, CancellationToken ct)
    {
        if (!command.IsActive && command.UserId == currentUser.UserId)
            return UserErrors.CannotDeactivateSelf;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        if (!command.IsActive)
        {
            var guarded = await AdministratorGuard.EnsureAnotherAdministratorAsync(roles, roleAssignments, command.UserId, ct);
            if (guarded.IsFailure)
                return guarded.Error;
        }

        var changed = await accounts.SetActiveAsync(command.UserId, command.IsActive, ct);
        if (changed.IsFailure)
            return changed.Error;
        if (!command.IsActive)
            await SessionRevocation.RevokeAllAsync(sessions, command.UserId, SessionRevokeReason.UserDeactivated, clock.GetUtcNow().UtcDateTime, ct);

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ActiveChanged(logger, command.UserId, command.IsActive, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4011, Level = LogLevel.Information, Message = "User {UserId} active set to {IsActive} by {ActorId}")]
    private static partial void ActiveChanged(ILogger logger, Guid userId, bool isActive, Guid? actorId);
}
