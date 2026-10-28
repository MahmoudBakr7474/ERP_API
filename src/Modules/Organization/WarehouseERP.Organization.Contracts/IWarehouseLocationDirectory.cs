namespace WarehouseERP.Organization.Contracts;

/// <summary>Read-only view of warehouse locations for other modules (Inventory).</summary>
public interface IWarehouseLocationDirectory
{
    /// <summary>The locations among these ids that exist, in any warehouse; unknown ids are left out.</summary>
    Task<IReadOnlyList<WarehouseLocationInfo>> FindManyAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken ct);

    /// <summary>The warehouse's DEFAULT location, or null when no warehouse has this id.</summary>
    Task<WarehouseLocationInfo?> FindDefaultAsync(Guid warehouseId, CancellationToken ct);
}
