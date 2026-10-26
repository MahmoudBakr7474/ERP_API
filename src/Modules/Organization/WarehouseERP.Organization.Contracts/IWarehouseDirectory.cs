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

/// <summary>A warehouse as other modules see it.</summary>
/// <param name="Id">Warehouse id.</param>
/// <param name="BranchId">The branch that owns it.</param>
/// <param name="Code">Code, unique within the branch.</param>
/// <param name="Type">Physical or transit.</param>
/// <param name="AllowsSelling">Whether stock here may be sold; always false for transit.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record WarehouseInfo(
    Guid Id, Guid BranchId, string Code, WarehouseType Type, bool AllowsSelling, bool IsActive);

/// <summary>Read-only view of branches for other modules.</summary>
public interface IBranchDirectory
{
    /// <summary>The branches among these ids that exist; unknown ids are left out.</summary>
    Task<IReadOnlyList<BranchInfo>> FindManyAsync(IReadOnlyCollection<Guid> branchIds, CancellationToken ct);
}

/// <summary>A branch as other modules see it.</summary>
/// <param name="Id">Branch id.</param>
/// <param name="CompanyId">The company it belongs to.</param>
/// <param name="Code">Code, unique within the company.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record BranchInfo(Guid Id, Guid CompanyId, string Code, string NameAr, bool IsActive);
