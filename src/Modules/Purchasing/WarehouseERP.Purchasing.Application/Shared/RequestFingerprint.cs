using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseERP.Purchasing.Application.Shared;

// What makes two posting requests the same: the action, what it acts on, the version the client saw (when it sends one),
// who sent it and, for a goods receipt, what it receives. A retry repeats all of them; anything else with the same key is
// another request.
internal static class RequestFingerprint
{
    public static string Of(string operation, Guid subjectId, byte[]? rowVersion, Guid? userId, string? content = null)
    {
        var version = rowVersion is null ? "" : Convert.ToBase64String(rowVersion);
        var request = string.Create(CultureInfo.InvariantCulture, $"{operation}|{subjectId:N}|{version}|{userId:N}");
        if (content is not null)
            request += "|" + content;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));
    }
}
