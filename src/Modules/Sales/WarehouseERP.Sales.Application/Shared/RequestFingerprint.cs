using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseERP.Sales.Application.Shared;

// What makes two posting requests the same: the action, what it acts on, the version the client saw, and who sent it. A
// retry repeats all of them; anything else with the same key is another request.
internal static class RequestFingerprint
{
    public static string Of(string operation, Guid subjectId, byte[]? rowVersion, Guid? userId)
    {
        var version = rowVersion is null ? "" : Convert.ToBase64String(rowVersion);
        var request = string.Create(CultureInfo.InvariantCulture, $"{operation}|{subjectId:N}|{version}|{userId:N}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request)));
    }
}
