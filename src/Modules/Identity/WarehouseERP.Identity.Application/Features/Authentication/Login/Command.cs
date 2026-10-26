namespace WarehouseERP.Identity.Application.Features.Authentication.Login;

public sealed record LoginCommand(string UserName, string Password);

/// <param name="Token">Goes in the HttpOnly session cookie, never to JavaScript.</param>
public sealed record WebSignIn(Guid UserId, Guid SessionId, string Token, DateTime ExpiresAtUtc);
