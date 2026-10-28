using System.Globalization;
using System.Text;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// The key two supplier names share when they are probably the same business, for duplicate warnings (never a uniqueness
/// rule): accents and Arabic diacritics, hamza forms and tatweel are dropped, ة reads as ه and ى as ي, case and
/// punctuation are ignored, and legal-form words (شركة, مؤسسة, Co, Ltd, LLC ...) are left out.
/// </summary>
public static class SupplierNames
{
    public const int MaxKeyLength = 200;

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
}
