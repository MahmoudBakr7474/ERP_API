using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Counts;

/// <summary>Write side of stock counts with their scope, lines and history (one aggregate), and the balances they freeze.</summary>
public interface IStockCountRepository
{
    /// <summary>The count's warehouse, or null when no count has this id.</summary>
    Task<Guid?> FindWarehouseIdAsync(Guid countId, CancellationToken ct);

    /// <summary>
    /// The count with its scope and lines, tracked and locked against other writers until the transaction ends, or null.
    /// With a row version, the save fails with a conflict unless the count still has it.
    /// </summary>
    Task<StockCount?> GetForUpdateAsync(Guid countId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>True when another count's active scope covers any of this count's scope.</summary>
    Task<bool> OverlapsActiveScopeAsync(StockCount count, CancellationToken ct);

    /// <summary>The non-zero available balances inside the count's scope, now.</summary>
    Task<IReadOnlyList<CountSnapshotLine>> ReadScopeBalancesAsync(StockCount count, CancellationToken ct);

    void Add(StockCount count);

    /// <summary>The save point; a count whose lines or history changed is saved as changed, so its row version moves.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
