using System.Globalization;
using System.Text;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// Keys two customers share when they are probably the same, for duplicate warnings (never a uniqueness rule). A name
/// key drops accents and Arabic diacritics, hamza forms and tatweel, reads ة as ه and ى as ي, ignores case and
/// punctuation, and leaves out legal-form words (شركة, مؤسسة, Co, Ltd, LLC ...). A phone key keeps the digits only.
/// </summary>
public static class CustomerNames
{
    public const int MaxKeyLength = 200;
    public const int MaxPhoneKeyLength = 30;

    private static readonly HashSet<string> LegalForms = new(StringComparer.Ordinal)
    {
        "شركه", "موسسه", "CO", "COMPANY", "CORP", "CORPORATION", "LTD", "LIMITED", "LLC", "INC", "SAE", "SA",
    };

    public static string Key(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var plain = new StringBuilder(name.Length);
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark || c == 'ـ')
                continue;
            var mapped = c switch
            {
                'ٱ' => 'ا', // alef wasla -> alef
                'ة' => 'ه', // teh marbuta -> heh
                'ى' => 'ي', // alef maksura -> yeh
                _ => char.IsDigit(c) ? (char)('0' + (int)CharUnicodeInfo.GetDecimalDigitValue(c)) : char.ToUpperInvariant(c),
            };
            plain.Append(char.IsLetterOrDigit(mapped) ? mapped : ' ');
        }

        var words = plain.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var significant = words.Where(w => !LegalForms.Contains(w)).ToArray();
        var key = string.Join(' ', significant.Length > 0 ? significant : words);
        return key.Length <= MaxKeyLength ? key : key[..MaxKeyLength];
    }

    /// <summary>The phone number's digits (Arabic digits read as digits), or null when it has none.</summary>
    public static string? PhoneKey(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;
        var digits = new string(phone.Where(char.IsDigit).Select(c => (char)('0' + (int)CharUnicodeInfo.GetDecimalDigitValue(c))).ToArray());
        if (digits.Length == 0)
            return null;
        return digits.Length <= MaxPhoneKeyLength ? digits : digits[..MaxPhoneKeyLength];
    }
}
