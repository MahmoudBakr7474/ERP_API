using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Identity.Domain;

public enum SessionClientType : byte
{
    /// <summary>Browser (React): the token travels in an HttpOnly cookie.</summary>
    Web = 1,

    /// <summary>POS or API tool: a short bearer access token plus a rotating refresh token.</summary>
    Api = 2,
}

public enum SessionRevokeReason : byte
{
    Logout = 1,
    RevokedByUser = 2,
    RevokedByAdministrator = 3,
    PasswordChanged = 4,
    PasswordReset = 5,
    UserDeactivated = 6,
    RefreshTokenReused = 7,
}

/// <summary>
/// A server-side sign-in. Only SHA-256 hashes of its tokens are stored. Web sessions slide with use until the idle
/// timeout and end at their absolute expiry; API sessions use 15-minute access tokens renewed by a refresh token
/// that rotates on every use, and presenting an already-used refresh token ends the session (stolen copy).
/// </summary>
public sealed class UserSession
{
    private UserSession() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SessionClientType ClientType { get; private set; }
    public string? DeviceId { get; private set; }
    public byte[] TokenHash { get; private set; } = [];
    public DateTime? TokenExpiresAtUtc { get; private set; }
    public byte[]? RefreshTokenHash { get; private set; }
    public byte[]? PreviousRefreshTokenHash { get; private set; }
    public DateTime? RefreshExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastSeenAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public SessionRevokeReason? RevokedReason { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public static UserSession StartWeb(Guid userId, byte[] tokenHash, DateTime now, TimeSpan absoluteLifetime) => new()
    {
        UserId = userId,
        ClientType = SessionClientType.Web,
        TokenHash = tokenHash,
        CreatedAtUtc = now,
        LastSeenAtUtc = now,
        ExpiresAtUtc = now + absoluteLifetime,
    };

    public static UserSession StartApi(Guid userId, string? deviceId, IssuedApiTokens tokens, DateTime now, TimeSpan absoluteLifetime)
    {
        var session = new UserSession
        {
            UserId = userId,
            ClientType = SessionClientType.Api,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now + absoluteLifetime,
        };
        session.Use(tokens);
        return session;
    }

    /// <summary>Whether a request carrying this session's token may proceed now.</summary>
    public bool AcceptsToken(DateTime now, TimeSpan webIdleTimeout) =>
        !IsRevoked && now < ExpiresAtUtc && ClientType switch
        {
            SessionClientType.Web => now - LastSeenAtUtc < webIdleTimeout,
            SessionClientType.Api => now < TokenExpiresAtUtc,
            _ => false,
        };

    /// <summary>
    /// Exchanges the current refresh token for new tokens. An already-used refresh token ends the session.
    /// </summary>
    public Result Refresh(byte[] presentedRefreshHash, IssuedApiTokens next, DateTime now)
    {
        if (ClientType != SessionClientType.Api || IsRevoked || now >= ExpiresAtUtc)
            return IdentityErrors.SessionRevoked;
        if (PreviousRefreshTokenHash is not null && SessionTokens.HashesEqual(PreviousRefreshTokenHash, presentedRefreshHash))
        {
            Revoke(SessionRevokeReason.RefreshTokenReused, now);
            return IdentityErrors.RefreshTokenReused;
        }
        if (RefreshTokenHash is null || !SessionTokens.HashesEqual(RefreshTokenHash, presentedRefreshHash) || now >= RefreshExpiresAtUtc)
            return IdentityErrors.SessionRevoked;

        PreviousRefreshTokenHash = RefreshTokenHash;
        Use(next);
        LastSeenAtUtc = now;
        return Result.Success();
    }

    public void Revoke(SessionRevokeReason reason, DateTime now)
    {
        if (IsRevoked) return;
        RevokedAtUtc = now;
        RevokedReason = reason;
    }

    private void Use(IssuedApiTokens tokens)
    {
        TokenHash = tokens.AccessTokenHash;
        TokenExpiresAtUtc = tokens.AccessExpiresAtUtc;
        RefreshTokenHash = tokens.RefreshTokenHash;
        RefreshExpiresAtUtc = tokens.RefreshExpiresAtUtc;
    }
}

/// <summary>Hashes of a freshly issued access/refresh pair and when each expires.</summary>
public sealed record IssuedApiTokens(byte[] AccessTokenHash, DateTime AccessExpiresAtUtc, byte[] RefreshTokenHash, DateTime RefreshExpiresAtUtc);

/// <summary>256-bit random tokens; only their SHA-256 hash is ever stored.</summary>
public static class SessionTokens
{
    public static (string Token, byte[] Hash) Create()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static bool HashesEqual(byte[] left, byte[] right) => CryptographicOperations.FixedTimeEquals(left, right);
}
