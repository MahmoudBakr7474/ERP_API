namespace WarehouseERP.Organization.Application.Contracts.Reports;

/// <summary>A branch of the organizational structure with its warehouses (ordered by code).</summary>
public sealed record StructureBranchResponse(
    Guid Id, string Code, string NameAr, string? NameEn, bool IsActive, IReadOnlyList<StructureWarehouseResponse> Warehouses);
