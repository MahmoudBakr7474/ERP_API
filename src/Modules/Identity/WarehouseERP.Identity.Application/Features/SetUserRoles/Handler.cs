using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.SetUserRoles;

// The new roles apply on the user's next request (access is resolved per request); no re-login needed.
public sealed partial class SetUserRolesHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<SetUserRolesHandler> logger)
    : ICommandHandler<SetUserRolesCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUserRolesCommand command, CancellationToken ct)
    {
        if (await accounts.FindAsync(command.UserId, ct) is null)
            return IdentityApplicationErrors.UserNotFound;
        var granted = await PrivilegeGuard.GetRolesAsync(roles, command.RoleIds, ct);
        if (granted.IsFailure)
            return granted.Error;
        if (!PrivilegeGuard.HoldsAll(currentUser, granted.Value, catalog))
            return IdentityApplicationErrors.ExceedsOwnPermissions;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, accounts, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        if (!granted.Value.Any(r => r.GrantsAllPermissions))
        {
            var guarded = await AdministratorGuard.EnsureAnotherAdministratorAsync(roles, accounts, command.UserId, ct);
            if (guarded.IsFailure)
                return guarded.Error;
        }

        var assigned = await accounts.SetRolesAsync(command.UserId, command.RoleIds, ct);
        if (assigned.IsFailure)
            return assigned.Error;
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RolesChanged(logger, command.UserId, command.RoleIds, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Roles of user {UserId} set to {RoleIds} by {ActorId}")]
    private static partial void RolesChanged(ILogger logger, Guid userId, IReadOnlyList<Guid> roleIds, Guid? actorId);
}
