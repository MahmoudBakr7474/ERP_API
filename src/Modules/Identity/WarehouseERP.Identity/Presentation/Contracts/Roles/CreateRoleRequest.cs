namespace WarehouseERP.Identity.Presentation.Contracts.Roles;

/// <param name="Permissions">Optional in JSON; missing means none.</param>
public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions);
