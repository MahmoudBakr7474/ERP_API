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
}
