namespace WarehouseERP.Identity.Presentation.Contracts.Users;

public sealed record SetRolesRequest(IReadOnlyList<Guid>? RoleIds);
