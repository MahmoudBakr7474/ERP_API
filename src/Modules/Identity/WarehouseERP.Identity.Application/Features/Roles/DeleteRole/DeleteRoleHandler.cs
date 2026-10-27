using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.DeleteRole;

// Only an unused, non-system role can be deleted; its history stays in the temporal tables.
public sealed partial class DeleteRoleHandler(
    ICurrentUser currentUser, IRoleRepository roles, IUserRoleAssignments roleAssignments, IIdentityPersistence persistence,
    ILogger<DeleteRoleHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<DeleteRoleCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(DeleteRoleCommand command, CancellationToken ct)
    {
        if (await roles.GetAsync(command.RoleId, ct) is not { } role)
            return RoleErrors.NotFound;
        var deletable = role.EnsureDeletable();
        if (deletable.IsFailure)
            return deletable.Error;
        if (await roleAssignments.AnyUserInRoleAsync(role.Id, ct))
            return RoleErrors.InUse;

        await roles.DeleteAsync(role.Id, ct);
        securityEvents.Record(SecurityEventType.RoleDeleted, roleId: role.Id, details: new { name = role.Name });
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleDeleted(logger, role.Id, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4022, Level = LogLevel.Information, Message = "Role {RoleId} deleted by {ActorId}")]
    private static partial void RoleDeleted(ILogger logger, Guid roleId, Guid? actorId);
}
