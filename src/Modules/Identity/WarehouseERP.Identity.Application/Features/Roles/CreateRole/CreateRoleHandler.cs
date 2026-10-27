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

namespace WarehouseERP.Identity.Application.Features.Roles.CreateRole;

public sealed partial class CreateRoleHandler(
    ICurrentUser currentUser, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<CreateRoleHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateRoleCommand command, CancellationToken ct)
    {
        // Roles apply company-wide, so only a company-wide caller defines them; assigning one to a user is checked
        // separately, against that user's scope.
        if (!currentUser.HasAllWarehouses)
            return await SecurityAudit.RefuseAsync(UserAccessErrors.ExceedsOwnScope, securityEvents, persistence, ct);
        var created = Role.Create(command.Name, command.Description, command.Permissions, PermissionCatalog.Names(catalog));
        if (created.IsFailure)
            return created.Error;
        var role = created.Value;
        if (!PrivilegeGuard.HoldsAll(currentUser, role.Permissions))
            return await SecurityAudit.RefuseAsync(PermissionErrors.ExceedsOwnPermissions, securityEvents, persistence, ct);
        if (await roles.NameExistsAsync(role.Name, excludingRoleId: null, ct))
            return RoleErrors.NameTaken;

        var roleId = await roles.AddAsync(role, ct);
        securityEvents.Record(SecurityEventType.RoleCreated, roleId: roleId, details: new { name = role.Name, permissions = role.Permissions });
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleCreated(logger, roleId, role.Permissions, currentUser.UserId);
        return roleId;
    }

    [LoggerMessage(EventId = 4020, Level = LogLevel.Information, Message = "Role {RoleId} created with permissions {Permissions} by {ActorId}")]
    private static partial void RoleCreated(ILogger logger, Guid roleId, IReadOnlySet<string> permissions, Guid? actorId);
}
