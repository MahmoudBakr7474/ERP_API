namespace WarehouseERP.Organization.Contracts;

// Read-only view of warehouses for other modules (Inventory, Sales, POS).
public interface IWarehouseDirectory
{
    Task<WarehouseInfo?> FindAsync(Guid warehouseId, CancellationToken ct);
}

public sealed record WarehouseInfo(
    Guid Id, Guid BranchId, string Code, WarehouseType Type, bool AllowsSelling, bool IsActive);
