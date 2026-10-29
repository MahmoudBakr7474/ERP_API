namespace WarehouseERP.Pos.Domain;

/// <summary>
/// Precision of POS numbers, the same as Sales': quantities decimal(18,4), percentages decimal(9,4), amounts decimal(19,4)
/// rounded to the currency's minor unit, half away from zero. Prices, discounts and VAT are computed by Sales
/// (ISalesPricing); POS only adds them up, checks what the cashier enters and shares a sold line's amounts out over the
/// units that come back.
/// </summary>
public static class Money
{
    public const int QuantityScale = 4;
    public const int PercentScale = 4;
    public const int AmountScale = 4;

    /// <summary>The largest quantity one line may carry.</summary>
    public const decimal MaxQuantity = 1_000_000_000m;

    /// <summary>The largest amount POS records anywhere, so every total fits decimal(19,4).</summary>
    public const decimal MaxAmount = 100_000_000_000_000m;

    public static decimal Round(decimal amount, int decimals) => decimal.Round(amount, decimals, MidpointRounding.AwayFromZero);

    public static bool FitsScale(decimal value, int scale) => decimal.Round(value, scale) == value;

    /// <summary>An amount of money as entered: above 0 (or 0 when allowed), no more decimals than the currency has.</summary>
    public static bool IsAmount(decimal value, int currencyDecimals, bool allowZero = false) =>
        (allowZero ? value >= 0 : value > 0) && value <= MaxAmount && FitsScale(value, currencyDecimals);

    /// <summary>
    /// The part of a sold line's <paramref name="amount"/> carried by its first <paramref name="units"/> of
    /// <paramref name="soldUnits"/>, rounded to the currency. Taking differences of these cumulative shares gives each
    /// return its part, and the last units back take exactly what is left, so returns never add up to more than was sold.
    /// </summary>
    public static decimal CumulativeShare(decimal amount, decimal units, decimal soldUnits, int currencyDecimals) =>
        units >= soldUnits ? amount : Round(amount * units / soldUnits, currencyDecimals);
}
