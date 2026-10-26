namespace WarehouseERP.Identity.Application.Features.Users.CreateUser;

public sealed record CreateUserCommand(
    string UserName, string DisplayName, string? Email, string Password, IReadOnlyList<Guid> RoleIds,
    bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
