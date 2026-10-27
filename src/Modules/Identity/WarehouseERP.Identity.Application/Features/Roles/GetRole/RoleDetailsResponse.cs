namespace WarehouseERP.Identity.Application.Features.Roles.GetRole;

/// <param name="GrantsAllPermissions">The Administrator role: <paramref name="Permissions"/> is the whole catalog.</param>
/// <param name="Version">Send it back with an update; it changes whenever the role does.</param>
public sealed record RoleDetailsResponse(
    Guid Id, string Name, string? Description, bool IsSystem, bool GrantsAllPermissions, IReadOnlyList<string> Permissions,
    string Version);
