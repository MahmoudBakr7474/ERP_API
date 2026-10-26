namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Users;

public sealed record NewUserAccount(string UserName, string DisplayName, string? Email, string Password);
