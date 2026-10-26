using WarehouseERP.Identity.Application.Contracts.UserAccess;

namespace WarehouseERP.Identity.Application.Contracts.Users;

public sealed record UserDetailsResponse(
    Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut,
    IReadOnlyList<UserRoleResponse> Roles, UserAccessResponse Access);
