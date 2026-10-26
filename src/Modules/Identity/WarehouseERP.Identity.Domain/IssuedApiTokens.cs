namespace WarehouseERP.Identity.Domain;

/// <summary>Hashes of a freshly issued access/refresh pair and when each expires.</summary>
public sealed record IssuedApiTokens(byte[] AccessTokenHash, DateTime AccessExpiresAtUtc, byte[] RefreshTokenHash, DateTime RefreshExpiresAtUtc);
