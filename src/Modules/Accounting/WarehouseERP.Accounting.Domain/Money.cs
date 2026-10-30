namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// Precision of ledger amounts: decimal(19,4) columns holding values in the base currency's minor unit (ISO 4217; two
/// decimals unless the currency is known to differ). An amount with more decimals is refused, never rounded silently.
/// </summary>
public static class Money
{
    /// <summary>The largest amount one journal line may carry, so totals always fit decimal(19,4).</summary>
    public const decimal MaxLineAmount = 100_000_000_000_000m;

    /// <summary>Decimals of a currency's minor unit (ISO 4217); two unless the currency is known to differ.</summary>
    public static int CurrencyDecimals(string currencyCode) => currencyCode.ToUpperInvariant() switch
    {
        "BHD" or "IQD" or "JOD" or "KWD" or "LYD" or "OMR" or "TND" => 3,
        "CLP" or "ISK" or "JPY" or "KRW" or "PYG" or "UGX" or "VND" or "XAF" or "XOF" => 0,
        _ => 2,
    };

    public static bool FitsScale(decimal value, int scale) => decimal.Round(value, scale) == value;
}
