namespace WarehouseERP.Identity.Presentation.Contracts.Users;

public sealed record UpdateUserRequest(string DisplayName, string? Email);
