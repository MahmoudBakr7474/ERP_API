namespace WarehouseERP.Inventory.Contracts;

/// <summary>
/// What a till may still sell, read without locks: a hint for the cashier, never a promise. Posting checks the same facts
/// again under the warehouse lock (IPosStockPosting).
/// </summary>
public interface IStockAvailabilityLookup
{
    /// <summary>
    /// For each of these products: the warehouse's sellable and reserved stock and what the given location holds. Products
    /// without stock come back with zeros.
    /// </summary>
    Task<IReadOnlyList<StockAvailability>> GetAsync(
        Guid warehouseId, Guid locationId, IReadOnlyCollection<Guid> productIds, CancellationToken ct);
}
