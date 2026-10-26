using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.UnlockUser;

public sealed class UnlockUserHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IRoleRepository roles, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence)
    : ICommandHandler<UnlockUserCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(UnlockUserCommand command, CancellationToken ct)
    {
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, accounts, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        var unlocked = await accounts.UnlockAsync(command.UserId, ct);
        if (unlocked.IsFailure)
            return unlocked.Error;
        var saved = await persistence.SaveChangesAsync(ct);
        return saved.IsFailure ? saved.Error : Done.Value;
    }
}
