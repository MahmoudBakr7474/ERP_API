using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.CreateUser;

// Account, roles and scope are saved together: a user never exists half-configured. The endpoint requires
// identity.users.manage; giving the new user any scope also needs identity.access.manage, as setting it later would.
public sealed partial class CreateUserHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access,
    IBranchDirectory branches, IWarehouseDirectory warehouses, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<CreateUserHandler> logger)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var grantsScope = command.IsCompanyWide || command.BranchIds.Count > 0 || command.WarehouseIds.Count > 0;
        if (grantsScope && !currentUser.HasPermission(IdentityPermissions.AccessManage))
            return UserAccessErrors.ManageRequired;
        var granted = await PrivilegeGuard.GetRolesAsync(roles, command.RoleIds, ct);
        if (granted.IsFailure)
            return granted.Error;
        if (!PrivilegeGuard.HoldsAll(currentUser, granted.Value, catalog))
            return PermissionErrors.ExceedsOwnPermissions;
        var scope = await PrivilegeGuard.EnsureGrantableScopeAsync(
            currentUser, access, branches, warehouses, command.IsCompanyWide, command.BranchIds, command.WarehouseIds, ct);
        if (scope.IsFailure)
            return scope.Error;

        var created = await accounts.CreateAsync(
            new NewUserAccount(command.UserName.Trim(), command.DisplayName.Trim(), command.Email, command.Password), ct);
        if (created.IsFailure)
            return created.Error;
        var userId = created.Value;

        var assigned = await roleAssignments.SetRolesAsync(userId, command.RoleIds, ct);
        if (assigned.IsFailure)
            return assigned.Error;
        access.Add(UserAccess.Create(userId, command.IsCompanyWide, command.BranchIds, command.WarehouseIds));

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        UserCreated(logger, userId, command.RoleIds.Count, command.IsCompanyWide, currentUser.UserId);
        return userId;
    }

    [LoggerMessage(EventId = 4010, Level = LogLevel.Information,
        Message = "User {UserId} created with {RoleCount} role(s), company-wide: {IsCompanyWide}, by {ActorId}")]
    private static partial void UserCreated(ILogger logger, Guid userId, int roleCount, bool isCompanyWide, Guid? actorId);
}
