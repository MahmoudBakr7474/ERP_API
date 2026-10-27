using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.UnlockUser;

public sealed class UnlockUserHandler(
    ICurrentUser currentUser, IUserCredentials credentials, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ISecurityEventLog securityEvents)
    : ICommandHandler<UnlockUserCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UnlockUserCommand command, CancellationToken ct)
    {
        // The lockout is what stops a stolen session from guessing the current password (POST /me/password); lifting
        // it for oneself would undo that.
        if (command.UserId == currentUser.UserId)
            return UserErrors.CannotUnlockSelf;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, access, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return await SecurityAudit.RefuseAsync(allowed.Error, securityEvents, persistence, ct, command.UserId);
        var unlocked = await credentials.UnlockAsync(command.UserId, ct);
        if (unlocked.IsFailure)
            return unlocked.Error;
        securityEvents.Record(SecurityEventType.UserUnlocked, command.UserId);
        var saved = await persistence.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Done.Value;
    }
}
