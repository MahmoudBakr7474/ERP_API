namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>Balances, availability and the movement ledger.</summary>
public interface IStockLedger
{
    /// <summary>
    /// Applies a posting's changes inside its transaction: availability per product and warehouse, then balances per
    /// location, each in a fixed key order and with atomic conditional updates that never go below zero, then one
    /// movement per change. Stops at the first shortage, which the caller rolls back.
    /// </summary>
    Task<StockLedgerOutcome> ApplyAsync(StockPosting posting, CancellationToken ct);

    /// <summary>
    /// Raises a warehouse's reserved quantity with an atomic conditional update that matches only while that much is on
    /// hand and not reserved yet; false when it isn't (nothing changed).
    /// </summary>
    Task<bool> TryReserveAsync(Guid productId, Guid warehouseId, decimal baseQuantity, CancellationToken ct);

    /// <summary>Lowers a warehouse's reserved quantity by what a reservation gave back.</summary>
    Task ReleaseReservedAsync(Guid productId, Guid warehouseId, decimal baseQuantity, CancellationToken ct);
}
