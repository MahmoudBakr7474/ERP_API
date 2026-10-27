using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Persistence;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.SecurityEvents;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.UserAccess;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.Identity.Application.Shared;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Users.SetUserAccess;

// Replaces where the user may act; applies on their next request.
public sealed partial class SetUserAccessHandler(
    ICurrentUser currentUser, IUserAccounts accounts, IUserRoleAssignments roleAssignments, IRoleRepository roles, IUserAccessRepository access,
    IBranchDirectory branches, IWarehouseDirectory warehouses, IEnumerable<PermissionDefinition> catalog,
    IIdentityPersistence persistence, ILogger<SetUserAccessHandler> logger, ISecurityEventLog securityEvents)
    : ICommandHandler<SetUserAccessCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetUserAccessCommand command, CancellationToken ct)
    {
        if (await accounts.FindAsync(command.UserId, ct) is null)
            return UserErrors.NotFound;
        var allowed = await PrivilegeGuard.EnsureCanManageAsync(currentUser, roleAssignments, roles, access, catalog, command.UserId, ct);
        if (allowed.IsFailure)
            return await SecurityAudit.RefuseAsync(allowed.Error, securityEvents, persistence, ct, command.UserId);
        var scope = await PrivilegeGuard.EnsureGrantableScopeAsync(
            currentUser, access, branches, warehouses, command.IsCompanyWide, command.BranchIds, command.WarehouseIds, ct);
        if (scope.IsFailure)
            return await SecurityAudit.RefuseAsync(scope.Error, securityEvents, persistence, ct, command.UserId);

        // Optimistic concurrency from the screen the caller saw: Revision is also the row's concurrency token, so a
        // change landing between this check and the save is refused by the database the same way.
        var existing = await access.GetAsync(command.UserId, ct);
        if ((existing?.Revision ?? 0) != command.Revision)
            return CommonErrors.ConcurrencyConflict;
        if (existing is { IsCompanyWide: true } && !command.IsCompanyWide)
        {
            var guarded = await AdministratorGuard.EnsureAnotherCompanyWideAdministratorAsync(roles, roleAssignments, command.UserId, ct);
            if (guarded.IsFailure)
                return guarded.Error;
        }
        if (existing is not null)
            existing.Set(command.IsCompanyWide, command.BranchIds, command.WarehouseIds);
        else
            access.Add(UserAccess.Create(command.UserId, command.IsCompanyWide, command.BranchIds, command.WarehouseIds));
        securityEvents.Record(SecurityEventType.UserAccessChanged, command.UserId, details: new
        {
            isCompanyWide = command.IsCompanyWide,
            branchIds = command.BranchIds,
            warehouseIds = command.WarehouseIds,
        });

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
