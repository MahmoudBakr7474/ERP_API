using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.UpdateRole;

// Renames the role and replaces its permissions; every holder gets the new set on their next request.
public sealed partial class UpdateRoleHandler(
    ICurrentUser currentUser, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<UpdateRoleHandler> logger)
    : ICommandHandler<UpdateRoleCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UpdateRoleCommand command, CancellationToken ct)
    {
        if (await roles.GetAsync(command.RoleId, ct) is not { } role)
            return IdentityApplicationErrors.RoleNotFound;
        var updated = role.Update(command.Name, command.Description, command.Permissions, PermissionCatalog.Names(catalog));
        if (updated.IsFailure)
            return updated.Error;
        if (!PrivilegeGuard.HoldsAll(currentUser, role.Permissions))
            return IdentityApplicationErrors.ExceedsOwnPermissions;
        if (await roles.NameExistsAsync(role.Name, excludingRoleId: role.Id, ct))
            return IdentityApplicationErrors.RoleNameTaken;

        await roles.UpdateAsync(role, ct);
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleUpdated(logger, role.Id, role.Permissions, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4021, Level = LogLevel.Information, Message = "Role {RoleId} updated, permissions now {Permissions}, by {ActorId}")]
    private static partial void RoleUpdated(ILogger logger, Guid roleId, IReadOnlySet<string> permissions, Guid? actorId);
}
