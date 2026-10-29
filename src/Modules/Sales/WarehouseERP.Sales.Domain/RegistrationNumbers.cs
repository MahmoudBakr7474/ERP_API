using System.Globalization;
using System.Text;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// Tax and commercial registration numbers as stored: upper-case ASCII letters and digits only. The separators people
/// type (spaces, dashes, dots, slashes) are dropped and Arabic-Indic digits are read as digits, so "123-456-789" and
/// "١٢٣ ٤٥٦ ٧٨٩" are the same number. Only Egypt's rule is known: its tax registration number is 9 digits; other countries
/// take any 1 to 30 letters and digits. The same rules as Purchasing's suppliers, kept here so the modules stay apart.
/// </summary>
public static class RegistrationNumbers
{
    public const int MaxLength = 30;
    public const string Egypt = "EG";

    /// <summary>The number as stored, or null when blank.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var kept = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (c is ' ' or '-' or '.' or '/' or '\\' or '_' or ' ')
                continue;
            kept.Append(char.IsDigit(c) ? (char)('0' + (int)CharUnicodeInfo.GetDecimalDigitValue(c)) : char.ToUpperInvariant(c));
        }
        return kept.ToString();
    }

    /// <summary>A normalized number of 1 to 30 ASCII letters and digits.</summary>
    public static bool IsValid(string normalized) =>
        normalized.Length is >= 1 and <= MaxLength && normalized.All(char.IsAsciiLetterOrDigit);

    /// <summary>A normalized tax registration number valid for the country.</summary>
    public static bool IsValidTaxNumber(string countryCode, string normalized) =>
        IsValid(normalized) && (countryCode != Egypt || (normalized.Length == 9 && normalized.All(char.IsAsciiDigit)));
}
