namespace WarehouseERP.Identity.Presentation.Contracts.Account;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
