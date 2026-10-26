namespace WarehouseERP.BuildingBlocks.Application.Security;

/// <summary>
/// The caller, whatever identity provider authenticated them. The host implements it from the
/// HTTP user; handlers use it for resource-level checks such as warehouse scope. Endpoint-level
/// permission checks are authorization policies in the host.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);

    /// <summary>True when the user may act on every warehouse.</summary>
    bool HasAllWarehouses { get; }

    /// <summary>Warehouses the user may act on; ignored when <see cref="HasAllWarehouses"/> is true.</summary>
    IReadOnlySet<Guid> WarehouseIds { get; }

    bool CanAccessWarehouse(Guid warehouseId) => HasAllWarehouses || WarehouseIds.Contains(warehouseId);
}
