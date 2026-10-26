namespace WarehouseERP.Identity.Application.Features.ChangeMyPassword;

/// <param name="CurrentSessionId">Kept signed in; every other session of the user ends.</param>
public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword, Guid CurrentSessionId);
