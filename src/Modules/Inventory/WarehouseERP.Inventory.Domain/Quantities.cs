namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// Precision of stored numbers. Quantities are decimal(18,4) in base units, unit costs and averages decimal(19,8),
/// values decimal(19,4) in the company's base currency. A quantity that does not fit is refused, never rounded; only
/// computed values and averages are rounded (half away from zero).
/// </summary>
public static class Quantities
{
    public const int QuantityScale = 4;
    public const int CostScale = 8;
    public const int ValueScale = 4;

    /// <summary>The largest base quantity one line may carry.</summary>
    public const decimal MaxQuantity = 1_000_000_000_000m;

    /// <summary>The largest unit cost, per entered unit and per base unit, so averages always fit decimal(19,8).</summary>
    public const decimal MaxUnitCost = 10_000_000_000m;

    /// <summary>The largest value one line may carry, so values always fit decimal(19,4).</summary>
    public const decimal MaxLineValue = 100_000_000_000_000m;

    public static bool FitsScale(decimal value, int scale) => decimal.Round(value, scale) == value;

    public static bool IsWhole(decimal value) => decimal.Truncate(value) == value;

    public static decimal RoundValue(decimal value) => decimal.Round(value, ValueScale, MidpointRounding.AwayFromZero);

    public static decimal RoundCost(decimal value) => decimal.Round(value, CostScale, MidpointRounding.AwayFromZero);
}
