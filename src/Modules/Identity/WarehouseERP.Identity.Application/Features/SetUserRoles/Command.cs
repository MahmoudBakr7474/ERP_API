namespace WarehouseERP.Identity.Application.Features.SetUserRoles;

/// <param name="RoleIds">The user's complete set of roles; roles not listed are removed.</param>
public sealed record SetUserRolesCommand(Guid UserId, IReadOnlyList<Guid> RoleIds);
