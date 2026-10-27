using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.UpdateUser;

public sealed class UpdateUserHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ISecurityEventLog securityEvents)
    : ICommandHandler<UpdateUserCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, access, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return await SecurityAudit.RefuseAsync(allowed.Error, securityEvents, persistence, ct, command.UserId);
        var updated = await accounts.UpdateProfileAsync(command.UserId, command.DisplayName.Trim(), command.Email, ct);
        if (updated.IsFailure)
            return updated.Error;
        securityEvents.Record(SecurityEventType.UserUpdated, command.UserId);
        var saved = await persistence.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Done.Value;
    }
}
