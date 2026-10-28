namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// The company-wide valuation of a product in the base currency: moving weighted average. Stock coming in adds
/// quantity and value and recomputes the average; stock going out removes quantity and value at the current average.
/// Moves between the company's own warehouses (transit included) change neither.
/// </summary>
public sealed class InventoryCostState
{
    private InventoryCostState() { } // EF Core

    public Guid ProductId { get; private set; }
    public decimal TotalQuantity { get; private set; }
    public decimal TotalValue { get; private set; }
    public decimal AverageUnitCost { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static InventoryCostState Start(Guid productId, DateTime nowUtc) =>
        new() { ProductId = productId, UpdatedAtUtc = nowUtc };

    /// <summary>Stock coming in with a known total value.</summary>
    public CostChange Receive(decimal quantity, decimal value, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        value = Quantities.RoundValue(value);
        TotalQuantity += quantity;
        TotalValue += value;
        AverageUnitCost = Quantities.RoundCost(TotalValue / TotalQuantity);
        UpdatedAtUtc = nowUtc;
        return new CostChange(quantity, value, Quantities.RoundCost(value / quantity));
    }

    /// <summary>Stock coming in at the current average, for an increase that has no cost of its own.</summary>
    public CostChange ReceiveAtAverage(decimal quantity, DateTime nowUtc) =>
        Receive(quantity, Quantities.RoundValue(quantity * AverageUnitCost), nowUtc);

    /// <summary>
    /// Stock going out at the current average; the average stays. The last units take whatever value is left, so value
    /// never outlives quantity.
    /// </summary>
    public CostChange Issue(decimal quantity, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (quantity > TotalQuantity)
            throw new InvalidOperationException(
                $"Product {ProductId} is valued for {TotalQuantity} but {quantity} went out: the ledger and the valuation disagree.");

        var value = quantity == TotalQuantity
            ? TotalValue
            : Math.Min(Quantities.RoundValue(quantity * AverageUnitCost), TotalValue);
        TotalQuantity -= quantity;
        TotalValue -= value;
        UpdatedAtUtc = nowUtc;
        return new CostChange(-quantity, -value, AverageUnitCost);
    }
}
