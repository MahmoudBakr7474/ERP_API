namespace WarehouseERP.Identity.Application.Contracts.Users;

public sealed record UserSummaryResponse(Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut);
