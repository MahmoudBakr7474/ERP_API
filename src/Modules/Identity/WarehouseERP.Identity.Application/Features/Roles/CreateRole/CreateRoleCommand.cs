namespace WarehouseERP.Identity.Application.Features.Roles.CreateRole;

/// <param name="Permissions">Names from the permission catalog (GET /api/identity/permissions).</param>
public sealed record CreateRoleCommand(string Name, string? Description, IReadOnlyList<string> Permissions);
