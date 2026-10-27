namespace WarehouseERP.Identity.Application.Features.Roles.UpdateRole;

/// <param name="Permissions">The role's complete set of permissions; names not listed are removed.</param>
/// <param name="Version">The role version the caller read (GET role → version); a newer one means someone else
/// changed the role meanwhile, and the update is refused instead of overwriting their change.</param>
public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description, IReadOnlyList<string> Permissions, string Version);
