using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Domain;

/// <summary>
/// What a barcode value must look like for its type. EAN-13 and GTIN follow GS1: fixed lengths of digits ending in a
/// mod-10 check digit. Internal and other barcodes are the company's own: any printable ASCII without spaces.
/// </summary>
public static class BarcodeFormat
{
    public const int MaxLength = 100; // varchar(100) column

    private static readonly int[] GtinLengths = [8, 12, 13, 14];

    public static bool IsValid(BarcodeType type, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
            return false;
        return type switch
        {
            BarcodeType.Ean13 => value.Length == 13 && HasGs1CheckDigit(value),
            BarcodeType.Gtin => GtinLengths.Contains(value.Length) && HasGs1CheckDigit(value),
            _ => value.All(c => c is > ' ' and <= '~'),
        };
    }

    // From the right, the digits before the check digit weigh 3, 1, 3, 1...; the check digit tops the sum up to a
    // multiple of 10.
    private static bool HasGs1CheckDigit(string value)
    {
        if (!value.All(char.IsAsciiDigit))
            return false;
        var sum = 0;
        for (var i = value.Length - 2; i >= 0; i--)
            sum += (value[i] - '0') * ((value.Length - 2 - i) % 2 == 0 ? 3 : 1);
        return (10 - sum % 10) % 10 == value[^1] - '0';
    }
}
