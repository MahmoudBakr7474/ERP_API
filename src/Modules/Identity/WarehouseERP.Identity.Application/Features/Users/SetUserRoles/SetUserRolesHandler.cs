using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.SetUserRoles;

// The new roles apply on the user's next request (access is resolved per request); no re-login needed.
public sealed partial class SetUserRolesHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<SetUserRolesHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<SetUserRolesCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUserRolesCommand command, CancellationToken ct)
    {
        if (await accounts.FindAsync(command.UserId, ct) is null)
            return UserErrors.NotFound;
        var granted = await PrivilegeGuard.GetRolesAsync(roles, command.RoleIds, ct);
        if (granted.IsFailure)
            return granted.Error;
        if (!PrivilegeGuard.HoldsAll(currentUser, granted.Value, catalog))
            return await SecurityAudit.RefuseAsync(PermissionErrors.ExceedsOwnPermissions, securityEvents, persistence, ct, command.UserId);
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, access, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return await SecurityAudit.RefuseAsync(allowed.Error, securityEvents, persistence, ct, command.UserId);
        if (!granted.Value.Any(r => r.GrantsAllPermissions))
        {
            var guarded = await AdministratorGuard.EnsureAnotherAdministratorAsync(roles, roleAssignments, access, command.UserId, ct);
            if (guarded.IsFailure)
                return guarded.Error;
        }

        var assigned = await roleAssignments.SetRolesAsync(command.UserId, command.RoleIds, ct);
        if (assigned.IsFailure)
            return assigned.Error;
        securityEvents.Record(SecurityEventType.UserRolesChanged, command.UserId, details: new { roleIds = command.RoleIds });
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RolesChanged(logger, command.UserId, command.RoleIds, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Roles of user {UserId} set to {RoleIds} by {ActorId}")]
    private static partial void RolesChanged(ILogger logger, Guid userId, IReadOnlyList<Guid> roleIds, Guid? actorId);
}
