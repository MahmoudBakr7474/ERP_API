namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// Precision of purchasing numbers. Quantities are decimal(18,4), unit prices decimal(19,8), percentages decimal(9,4) and
/// amounts decimal(19,4). A quantity, price or percentage that does not fit is refused, never rounded. Amounts are
/// rounded to the document currency's minor unit, half away from zero, line by line: gross, then discount, then tax;
/// a document's totals are the sums of its rounded lines.
/// </summary>
public static class Amounts
{
    public const int QuantityScale = 4;
    public const int PriceScale = 8;
    public const int PercentScale = 4;

    /// <summary>The largest quantity one line may carry.</summary>
    public const decimal MaxQuantity = 1_000_000_000m;

    /// <summary>The largest unit price, so inventory unit costs and averages always fit.</summary>
    public const decimal MaxUnitPrice = 10_000_000_000m;

    /// <summary>The largest amount one line may carry, so totals always fit decimal(19,4).</summary>
    public const decimal MaxLineAmount = 100_000_000_000_000m;

    /// <summary>Decimals of a currency's minor unit (ISO 4217); two unless the currency is known to differ.</summary>
    public static int CurrencyDecimals(string currencyCode) => currencyCode.ToUpperInvariant() switch
    {
        "BHD" or "IQD" or "JOD" or "KWD" or "LYD" or "OMR" or "TND" => 3,
        "CLP" or "ISK" or "JPY" or "KRW" or "PYG" or "UGX" or "VND" or "XAF" or "XOF" => 0,
        _ => 2,
    };

    public static decimal Round(decimal amount, int decimals) => decimal.Round(amount, decimals, MidpointRounding.AwayFromZero);

    public static decimal RoundPrice(decimal price) => decimal.Round(price, PriceScale, MidpointRounding.AwayFromZero);

    public static bool FitsScale(decimal value, int scale) => decimal.Round(value, scale) == value;

    public static bool IsWhole(decimal value) => decimal.Truncate(value) == value;
}
