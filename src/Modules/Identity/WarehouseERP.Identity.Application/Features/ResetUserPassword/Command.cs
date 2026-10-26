namespace WarehouseERP.Identity.Application.Features.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword);
