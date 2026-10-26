namespace WarehouseERP.Identity.Application.Features.Users.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword);
