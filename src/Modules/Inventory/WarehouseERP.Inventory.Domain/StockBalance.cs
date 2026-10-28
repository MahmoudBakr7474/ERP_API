using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// The quantity of a product in one location, lot and status. Posting changes it with atomic conditional updates
/// (never below zero) in the same transaction as its movements; nothing else writes it.
/// </summary>
public sealed class StockBalance
{
    private StockBalance() { } // EF Core

    public long Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid? LotId { get; private set; }
    public StockStatus StockStatus { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
