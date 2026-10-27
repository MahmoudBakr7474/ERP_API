using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

/// <param name="Result">Success, or why the password was not changed.</param>
/// <param name="LockoutStarted">This wrong current password reached the lockout threshold.</param>
public sealed record PasswordChange(Result Result, bool LockoutStarted = false);
