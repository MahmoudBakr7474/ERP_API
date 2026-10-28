using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// One posted change of stock in one location: the quantity ledger. Signed base quantity, no cost (valuation lives in
/// InventoryCostEntry). Written once by posting, never changed.
/// </summary>
public sealed class StockMovement
{
    private StockMovement() { } // EF Core

    public long Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid LineId { get; private set; }
    public Guid AllocationId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid? LotId { get; private set; }
    public StockStatus StockStatus { get; private set; }

    /// <summary>Base quantity: positive in, negative out.</summary>
    public decimal Quantity { get; private set; }

    public DateTime PostedAtUtc { get; private set; }

    public static StockMovement Record(
        Guid documentId, Guid lineId, Guid allocationId, Guid productId, Guid warehouseId, Guid locationId, Guid? lotId,
        StockStatus stockStatus, decimal quantity, DateTime postedAtUtc)
    {
        if (quantity == 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "A movement moves something.");
        return new StockMovement
        {
            DocumentId = documentId,
            LineId = lineId,
            AllocationId = allocationId,
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            LotId = lotId,
            StockStatus = stockStatus,
            Quantity = quantity,
            PostedAtUtc = postedAtUtc,
        };
    }
}
