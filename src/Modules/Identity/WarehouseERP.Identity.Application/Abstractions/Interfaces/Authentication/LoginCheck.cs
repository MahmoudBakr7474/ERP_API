namespace WarehouseERP.Identity.Application.Abstractions.Interfaces.Authentication;

public sealed record LoginCheck(LoginOutcome Outcome, Guid? UserId);
