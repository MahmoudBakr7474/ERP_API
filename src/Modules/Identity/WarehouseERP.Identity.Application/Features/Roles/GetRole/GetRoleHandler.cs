using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Identity.Application.Abstractions.Interfaces.Roles;
using WarehouseERP.Identity.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Features.Roles.GetRole;

public sealed class GetRoleHandler(IRoleRepository roles, IEnumerable<PermissionDefinition> catalog)
    : IQueryHandler<GetRoleQuery, RoleDetailsResponse>
{
    public async Task<Result<RoleDetailsResponse>> HandleAsync(GetRoleQuery query, CancellationToken ct)
    {
        if (await roles.GetAsync(query.RoleId, ct) is not { } role)
            return RoleErrors.NotFound;
        var permissions = role.GrantsAllPermissions ? catalog.Select(p => p.Name) : role.Permissions;
        return new RoleDetailsResponse(
            role.Id, role.Name, role.Description, role.IsSystem, role.GrantsAllPermissions,
            permissions.Order(StringComparer.Ordinal).ToList(), role.Version ?? "");
    }
}
