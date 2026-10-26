namespace WarehouseERP.Identity.Application.Features.SetUserActive;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive);
