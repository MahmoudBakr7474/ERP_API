using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.UpdateUser;

public sealed class UpdateUserHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence)
    : ICommandHandler<UpdateUserCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, accounts, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        var updated = await accounts.UpdateProfileAsync(command.UserId, command.DisplayName.Trim(), command.Email, ct);
        if (updated.IsFailure)
            return updated.Error;
        var saved = await persistence.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Done.Value;
    }
}
