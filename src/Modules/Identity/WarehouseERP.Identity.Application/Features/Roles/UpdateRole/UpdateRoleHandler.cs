using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.UpdateRole;

// Renames the role and replaces its permissions; every holder gets the new set on their next request.
public sealed partial class UpdateRoleHandler(
    ICurrentUser currentUser, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<UpdateRoleHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<UpdateRoleCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UpdateRoleCommand command, CancellationToken ct)
    {
        // Roles apply company-wide, so only a company-wide caller edits them.
        if (!currentUser.HasAllWarehouses)
            return await SecurityAudit.RefuseAsync(UserAccessErrors.ExceedsOwnScope, securityEvents, persistence, ct, roleId: command.RoleId);
        if (await roles.GetAsync(command.RoleId, ct) is not { } role)
            return RoleErrors.NotFound;
        var before = role.Permissions.ToList();
        var updated = role.Update(command.Name, command.Description, command.Permissions, PermissionCatalog.Names(catalog));
        if (updated.IsFailure)
            return updated.Error;
        // Both sets: nobody adds a permission they lack, or strips one from the role's holders.
        if (!PrivilegeGuard.HoldsAll(currentUser, before.Union(role.Permissions)))
            return await SecurityAudit.RefuseAsync(PermissionErrors.ExceedsOwnPermissions, securityEvents, persistence, ct, roleId: role.Id);
        // The version the caller edited; the repository makes the database check it again at the save.
        if (!role.IsAtVersion(command.Version))
            return CommonErrors.ConcurrencyConflict;
        if (await roles.NameExistsAsync(role.Name, excludingRoleId: role.Id, ct))
            return RoleErrors.NameTaken;

        await roles.UpdateAsync(role, ct);
        securityEvents.Record(SecurityEventType.RoleUpdated, roleId: role.Id, details: new { name = role.Name, permissions = role.Permissions });
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleUpdated(logger, role.Id, role.Permissions, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4021, Level = LogLevel.Information, Message = "Role {RoleId} updated, permissions now {Permissions}, by {ActorId}")]
    private static partial void RoleUpdated(ILogger logger, Guid roleId, IReadOnlySet<string> permissions, Guid? actorId);
}
