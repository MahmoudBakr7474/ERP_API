using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.DeleteRole;

// Only an unused, non-system role can be deleted; its history stays in the temporal tables.
public sealed partial class DeleteRoleHandler(
    ICurrentUser currentUser, IRoleRepository roles, IUserAccounts accounts, IIdentityPersistence persistence,
    ILogger<DeleteRoleHandler> logger)
    : ICommandHandler<DeleteRoleCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(DeleteRoleCommand command, CancellationToken ct)
    {
        if (await roles.GetAsync(command.RoleId, ct) is not { } role)
            return IdentityApplicationErrors.RoleNotFound;
        var deletable = role.EnsureDeletable();
        if (deletable.IsFailure)
            return deletable.Error;
        if (await accounts.AnyUserInRoleAsync(role.Id, ct))
            return IdentityApplicationErrors.RoleInUse;

        await roles.DeleteAsync(role.Id, ct);
        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        RoleDeleted(logger, role.Id, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4022, Level = LogLevel.Information, Message = "Role {RoleId} deleted by {ActorId}")]
    private static partial void RoleDeleted(ILogger logger, Guid roleId, Guid? actorId);
}
