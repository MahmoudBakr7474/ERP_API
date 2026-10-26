namespace WarehouseERP.Identity.Application.Features.Roles.GetRole;

public sealed record GetRoleQuery(Guid RoleId);

/// <param name="GrantsAllPermissions">The Administrator role: <paramref name="Permissions"/> is the whole catalog.</param>
public sealed record RoleDetails(
    Guid Id, string Name, string? Description, bool IsSystem, bool GrantsAllPermissions, IReadOnlyList<string> Permissions);
