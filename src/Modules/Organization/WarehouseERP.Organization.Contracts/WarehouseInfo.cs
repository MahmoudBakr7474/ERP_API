namespace WarehouseERP.Organization.Contracts;

/// <summary>A warehouse as other modules see it.</summary>
/// <param name="Id">Warehouse id.</param>
/// <param name="BranchId">The branch that owns it.</param>
/// <param name="Code">Code, unique within the branch.</param>
/// <param name="Type">Physical or transit.</param>
/// <param name="AllowsSelling">Whether stock here may be sold; always false for transit.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record WarehouseInfo(
    Guid Id, Guid BranchId, string Code, WarehouseType Type, bool AllowsSelling, bool IsActive);
