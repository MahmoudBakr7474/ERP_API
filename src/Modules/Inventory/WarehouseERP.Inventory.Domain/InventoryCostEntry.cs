namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// One valuation step for one document line, with the state it left: the financial history beside the quantity
/// ledger. Written once by posting, never changed; summing its deltas gives the current valuation.
/// </summary>
public sealed class InventoryCostEntry
{
    private InventoryCostEntry() { } // EF Core

    public long Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid LineId { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal ValueDelta { get; private set; }

    /// <summary>Cost per base unit this step used.</summary>
    public decimal UnitCost { get; private set; }

    public decimal QuantityAfter { get; private set; }
    public decimal ValueAfter { get; private set; }
    public decimal AverageCostAfter { get; private set; }
    public DateTime PostedAtUtc { get; private set; }

    public static InventoryCostEntry Record(InventoryCostState state, CostChange change, Guid documentId, Guid lineId, DateTime postedAtUtc) =>
        new()
        {
            ProductId = state.ProductId,
            DocumentId = documentId,
            LineId = lineId,
            QuantityDelta = change.QuantityDelta,
            ValueDelta = change.ValueDelta,
            UnitCost = change.UnitCost,
            QuantityAfter = state.TotalQuantity,
            ValueAfter = state.TotalValue,
            AverageCostAfter = state.AverageUnitCost,
            PostedAtUtc = postedAtUtc,
        };
}
