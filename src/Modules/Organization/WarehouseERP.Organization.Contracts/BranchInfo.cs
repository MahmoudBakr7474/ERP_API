namespace WarehouseERP.Organization.Contracts;

/// <summary>A branch as other modules see it.</summary>
/// <param name="Id">Branch id.</param>
/// <param name="CompanyId">The company it belongs to.</param>
/// <param name="Code">Code, unique within the company.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record BranchInfo(Guid Id, Guid CompanyId, string Code, string NameAr, bool IsActive);
