using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Features.Authentication;

// Shared by IssueApiToken and RefreshApiToken: the response they both return and how a token pair is issued.
// Area-level types like these are the only code use cases of one area share; use cases never reference each other.

public sealed record ApiTokens(
    Guid SessionId, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

internal sealed record IssuedPair(string AccessToken, string RefreshToken, IssuedApiTokens Hashes)
{
    public ApiTokens ToResponse(Guid sessionId) =>
        new(sessionId, AccessToken, Hashes.AccessExpiresAtUtc, RefreshToken, Hashes.RefreshExpiresAtUtc);
}

internal static class ApiTokenIssuer
{
    /// <summary>A new access/refresh pair; neither outlives the session.</summary>
    public static IssuedPair Issue(DateTime now, DateTime sessionExpiresAtUtc, IdentitySessionOptions settings)
    {
        var (access, accessHash) = SessionTokens.Create();
        var (refresh, refreshHash) = SessionTokens.Create();
        var accessExpires = Min(now + settings.ApiAccessTokenLifetime, sessionExpiresAtUtc);
        var refreshExpires = Min(now + settings.ApiRefreshTokenLifetime, sessionExpiresAtUtc);
        return new IssuedPair(access, refresh, new IssuedApiTokens(accessHash, accessExpires, refreshHash, refreshExpires));
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
