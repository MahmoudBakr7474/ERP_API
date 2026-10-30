namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>A row of the Categories sheet as text. An empty parent code means a top-level category.</summary>
public sealed record CategoryImportRow(int Row, string? Code, string? NameAr, string? NameEn, string? ParentCode);
