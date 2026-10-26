namespace WarehouseERP.Identity.Presentation.Contracts.Users;

public sealed record CreateUserRequest(
    string UserName, string DisplayName, string? Email, string Password, IReadOnlyList<Guid>? RoleIds,
    bool IsCompanyWide, IReadOnlyList<Guid>? BranchIds, IReadOnlyList<Guid>? WarehouseIds);
