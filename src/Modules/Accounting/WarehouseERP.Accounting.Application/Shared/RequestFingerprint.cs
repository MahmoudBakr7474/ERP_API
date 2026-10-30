using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseERP.Accounting.Application.Shared;

// What makes two posting requests the same: the action, the journal, the version the client read and who sent it. A retry
// repeats all of them; anything else with the same key is another request.
internal static class RequestFingerprint
{
    public static string Of(string operation, Guid subjectId, byte[] rowVersion, Guid? userId)
    {
        var text = string.Create(CultureInfo.InvariantCulture, $"{operation}|{subjectId:N}|{Convert.ToBase64String(rowVersion)}|{userId:N}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
