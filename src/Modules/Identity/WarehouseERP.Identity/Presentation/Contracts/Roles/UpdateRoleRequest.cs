namespace WarehouseERP.Identity.Presentation.Contracts.Roles;

/// <param name="Permissions">The complete set; optional in JSON, missing means none.</param>
/// <param name="Version">From GET role; the update is refused (409 Common.ConcurrencyConflict) if the role changed since.</param>
public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions, string Version);
