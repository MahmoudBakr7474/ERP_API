namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;

public sealed record UserAccount(Guid Id, string UserName, string DisplayName, string? Email, bool IsActive, bool IsLockedOut);
