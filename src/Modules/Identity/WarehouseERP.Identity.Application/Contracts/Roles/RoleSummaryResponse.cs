namespace WarehouseERP.Identity.Application.Contracts.Roles;

public sealed record RoleSummaryResponse(Guid Id, string Name, string? Description, bool IsSystem, int PermissionCount);
