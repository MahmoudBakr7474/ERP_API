namespace WarehouseERP.Identity.Application.Features.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string DisplayName, string? Email);
