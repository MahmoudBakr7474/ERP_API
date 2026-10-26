namespace WarehouseERP.Identity.Application.Features.IssueApiToken;

/// <param name="DeviceId">The POS terminal, when the caller is one.</param>
public sealed record IssueApiTokenCommand(string UserName, string Password, string? DeviceId);

public sealed record ApiTokens(
    Guid SessionId, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
