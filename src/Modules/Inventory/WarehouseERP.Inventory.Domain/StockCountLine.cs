namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// One product in one location of a count: the quantity the frozen balance expected (zero for something found that was
/// not expected) and the quantity counted, in base units.
/// </summary>
public sealed class StockCountLine
{
    private StockCountLine() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CountId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid LocationId { get; private set; }
    public decimal ExpectedQuantity { get; private set; }
    public decimal? CountedQuantity { get; private set; }

    /// <summary>Cost of one base unit for a surplus; without it a surplus comes in at the average cost.</summary>
    public decimal? UnitCost { get; private set; }

    /// <summary>Counted minus expected; zero until counted.</summary>
    public decimal Variance => CountedQuantity is { } counted ? counted - ExpectedQuantity : 0m;

    internal static StockCountLine Expected(Guid warehouseId, Guid productId, Guid locationId, decimal quantity) =>
        new() { WarehouseId = warehouseId, ProductId = productId, LocationId = locationId, ExpectedQuantity = quantity };

    internal void Count(decimal quantity, decimal? unitCost)
    {
        CountedQuantity = quantity;
        UnitCost = unitCost;
    }
}
