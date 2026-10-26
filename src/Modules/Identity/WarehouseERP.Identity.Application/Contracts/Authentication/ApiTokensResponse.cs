namespace WarehouseERP.Identity.Application.Contracts.Authentication;

/// <summary>What a POS or API client receives when it signs in or refreshes.</summary>
public sealed record ApiTokensResponse(
    Guid SessionId, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
