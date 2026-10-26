using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseERP.Identity.Domain;

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
