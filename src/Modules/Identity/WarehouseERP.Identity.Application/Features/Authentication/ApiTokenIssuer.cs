using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Application.Features.Authentication;

// Shared by IssueApiToken and RefreshApiToken (area-level: use cases never reference each other).
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
