namespace WarehouseERP.Identity.Presentation.Contracts.Roles;

public sealed record RoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions);
