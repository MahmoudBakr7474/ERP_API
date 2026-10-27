namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

/// <param name="LockoutStarted">This wrong password reached the lockout threshold.</param>
public sealed record LoginCheck(LoginOutcome Outcome, Guid? UserId, bool LockoutStarted = false);
