namespace WarehouseERP.Identity.Application.Features.Users.SetUserActive;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive);
