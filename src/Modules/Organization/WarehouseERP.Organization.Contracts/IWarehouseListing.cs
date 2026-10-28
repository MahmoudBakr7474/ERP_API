namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// Every active warehouse of a type, whatever the caller's scope: for choices that reach outside it, such as the
/// destination of a transfer. Carries names only; access to a warehouse's data is still checked where it is used.
/// </summary>
public interface IWarehouseListing
{
    /// <summary>Active warehouses of this type in active branches, by branch code then warehouse code.</summary>
    Task<IReadOnlyList<WarehouseListItem>> ListActiveAsync(WarehouseType type, CancellationToken ct);
}
