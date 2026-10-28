using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseERP.Inventory.Application.Shared;

// What makes two stock-changing requests the same: the action, what it acts on, the version the client saw, who sent it
// and, for a receipt, what it receives. A retry repeats all of them; anything else with the same key is another request.
internal static class RequestFingerprint
{
    public static string Of(string operation, Guid subjectId, byte[] rowVersion, Guid? userId, string? content = null)
    {
        var request = string.Create(CultureInfo.InvariantCulture, $"{operation}|{subjectId:N}|{Convert.ToBase64String(rowVersion)}|{userId:N}");
        if (content is not null)
            request += "|" + content;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));
    }
}
