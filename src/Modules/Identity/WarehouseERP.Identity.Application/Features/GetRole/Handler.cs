using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.GetRole;

public sealed class GetRoleHandler(IRoleRepository roles, IEnumerable<PermissionDefinition> catalog)
    : IQueryHandler<GetRoleQuery, RoleDetails>
{
    public async Task<Result<RoleDetails>> HandleAsync(GetRoleQuery query, CancellationToken ct)
    {
        if (await roles.GetAsync(query.RoleId, ct) is not { } role)
            return IdentityApplicationErrors.RoleNotFound;
        var permissions = role.GrantsAllPermissions ? catalog.Select(p => p.Name) : role.Permissions;
        return new RoleDetails(
            role.Id, role.Name, role.Description, role.IsSystem, role.GrantsAllPermissions,
            permissions.Order(StringComparer.Ordinal).ToList());
    }
}
