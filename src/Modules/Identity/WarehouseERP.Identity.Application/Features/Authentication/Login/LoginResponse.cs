namespace WarehouseERP.Identity.Application.Features.Authentication.Login;

/// <param name="Token">Goes in the HttpOnly session cookie, never to JavaScript.</param>
public sealed record LoginResponse(Guid UserId, Guid SessionId, string Token, DateTime ExpiresAtUtc);
