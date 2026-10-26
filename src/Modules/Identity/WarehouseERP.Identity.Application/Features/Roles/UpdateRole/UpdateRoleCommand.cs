namespace WarehouseERP.Identity.Application.Features.Roles.UpdateRole;

/// <param name="Permissions">The role's complete set of permissions; names not listed are removed.</param>
public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description, IReadOnlyList<string> Permissions);
