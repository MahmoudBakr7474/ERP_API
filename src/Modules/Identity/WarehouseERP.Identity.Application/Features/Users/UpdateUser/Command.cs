namespace WarehouseERP.Identity.Application.Features.Users.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string DisplayName, string? Email);
