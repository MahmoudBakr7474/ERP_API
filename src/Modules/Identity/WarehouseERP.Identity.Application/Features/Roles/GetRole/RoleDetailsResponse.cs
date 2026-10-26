namespace WarehouseERP.Identity.Application.Features.Roles.GetRole;

/// <param name="GrantsAllPermissions">The Administrator role: <paramref name="Permissions"/> is the whole catalog.</param>
public sealed record RoleDetailsResponse(
    Guid Id, string Name, string? Description, bool IsSystem, bool GrantsAllPermissions, IReadOnlyList<string> Permissions);
