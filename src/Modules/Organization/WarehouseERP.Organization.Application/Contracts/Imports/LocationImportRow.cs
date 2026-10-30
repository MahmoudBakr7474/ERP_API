namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>One uploaded row as text (trimmed, null when empty), numbered as in Excel.</summary>
/// <param name="BranchCode">With <paramref name="WarehouseCode"/>, names the warehouse: warehouse codes repeat across branches.</param>
public sealed record LocationImportRow(int Row, string? BranchCode, string? WarehouseCode, string? Code, string? NameAr, string? NameEn);
