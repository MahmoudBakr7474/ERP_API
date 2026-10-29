using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WarehouseERP.Pos.Application.Shared;

// What makes two requests the same: the action, everything the client sent (its key left out), and who sent it. A retry
// repeats all of them; anything else with the same key is another request.
internal static class RequestFingerprint
{
    public static string Of<TRequest>(string operation, TRequest request, Guid? userId)
    {
        var body = JsonSerializer.Serialize(request);
        var text = string.Create(CultureInfo.InvariantCulture, $"{operation}|{body}|{userId:N}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
