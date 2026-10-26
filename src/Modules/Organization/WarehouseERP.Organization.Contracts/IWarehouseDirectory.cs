namespace WarehouseERP.Organization.Contracts;

/// <summary>Read-only view of warehouses for other modules (Identity, Inventory, Sales, POS).</summary>
public interface IWarehouseDirectory
{
    /// <summary>The warehouse, or null when no warehouse has this id.</summary>
    Task<WarehouseInfo?> FindAsync(Guid warehouseId, CancellationToken ct);

    /// <summary>The warehouses among these ids that exist; unknown ids are left out.</summary>
    Task<IReadOnlyList<WarehouseInfo>> FindManyAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken ct);

    /// <summary>Ids of every warehouse in these branches, including ones created after a grant was made.</summary>
    Task<IReadOnlyList<Guid>> ListIdsInBranchesAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct);
}
