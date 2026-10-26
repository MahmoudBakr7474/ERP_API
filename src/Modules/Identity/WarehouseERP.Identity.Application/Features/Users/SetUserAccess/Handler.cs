using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.SetUserAccess;

// Replaces where the user may act; applies on their next request.
public sealed partial class SetUserAccessHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IRoleRepository roles, IUserAccessRepository access,
    IBranchDirectory branches, IWarehouseDirectory warehouses, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<SetUserAccessHandler> logger)
    : ICommandHandler<SetUserAccessCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUserAccessCommand command, CancellationToken ct)
    {
        if (await accounts.FindAsync(command.UserId, ct) is null)
            return IdentityApplicationErrors.UserNotFound;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, accounts, roles, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return allowed.Error;
        var scope = await PrivilegeGuard.EnsureGrantableScopeAsync(
            currentUser, access, branches, warehouses, command.IsCompanyWide, command.BranchIds, command.WarehouseIds, ct);
        if (scope.IsFailure)
            return scope.Error;

        if (await access.GetAsync(command.UserId, ct) is { } existing)
            existing.Set(command.IsCompanyWide, command.BranchIds, command.WarehouseIds);
        else
            access.Add(UserAccess.Create(command.UserId, command.IsCompanyWide, command.BranchIds, command.WarehouseIds));

        var saved = await persistence.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AccessChanged(logger, command.UserId, command.IsCompanyWide, command.BranchIds.Count, command.WarehouseIds.Count, currentUser.UserId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 4014, Level = LogLevel.Information,
        Message = "Access of user {UserId} set (company-wide: {IsCompanyWide}, {BranchCount} branch(es), {WarehouseCount} warehouse(s)) by {ActorId}")]
    private static partial void AccessChanged(ILogger logger, Guid userId, bool isCompanyWide, int branchCount, int warehouseCount, Guid? actorId);
}
