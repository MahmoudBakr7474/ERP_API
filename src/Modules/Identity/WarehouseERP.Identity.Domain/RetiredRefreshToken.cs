namespace WarehouseERP.Identity.Domain;

/// <summary>
/// The hash of a refresh token its session already exchanged. Kept so a copy coming back, however many rotations
/// later, is recognized as reuse and ends the session; one row per hash, so two parallel refreshes with the same
/// token can't both succeed.
/// </summary>
public sealed class RetiredRefreshToken
{
    private RetiredRefreshToken() { } // EF Core

    internal RetiredRefreshToken(byte[] tokenHash, DateTime retiredAtUtc)
    {
        TokenHash = tokenHash;
        RetiredAtUtc = retiredAtUtc;
    }

    public byte[] TokenHash { get; private set; } = [];
    public Guid SessionId { get; private set; }
    public DateTime RetiredAtUtc { get; private set; }
}
