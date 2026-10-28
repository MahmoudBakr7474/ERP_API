using System.Globalization;
using System.Text;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// A supplier invoice number as compared for duplicates: its letters and digits only, upper case, Arabic-Indic digits
/// read as digits. "inv-00/12", "INV 0012" and "INV0012" are the same invoice.
/// </summary>
public static class SupplierInvoiceNumbers
{
    public const int MaxLength = 50;

    /// <summary>The key, or null when the number has no letter or digit.</summary>
    public static string? Key(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return null;
        var kept = new StringBuilder(number.Length);
        foreach (var c in number)
        {
            if (char.IsDigit(c))
                kept.Append((char)('0' + (int)CharUnicodeInfo.GetDecimalDigitValue(c)));
            else if (char.IsLetter(c))
                kept.Append(char.ToUpperInvariant(c));
        }
        return kept.Length is > 0 and <= MaxLength ? kept.ToString() : null;
    }
}
