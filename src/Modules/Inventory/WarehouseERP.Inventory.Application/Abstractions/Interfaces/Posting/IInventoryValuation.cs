using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <summary>The company-wide valuation (moving weighted average) and its history.</summary>
public interface IInventoryValuation
{
    /// <summary>
    /// The valuation of these products, tracked and locked until the transaction ends, taken one product at a time in a
    /// fixed order. A product never valued is absent.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, InventoryCostState>> LockAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct);

    /// <summary>The value each line of a posted document moved (negative for stock that went out), by line id.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetLineValuesAsync(Guid documentId, CancellationToken ct);

    void Add(InventoryCostState state);

    void Add(InventoryCostEntry entry);
}
