namespace WarehouseERP.Identity.Application.Features.Logout;

/// <param name="SessionId">The caller's own session, from the authenticated request.</param>
public sealed record LogoutCommand(Guid SessionId);
