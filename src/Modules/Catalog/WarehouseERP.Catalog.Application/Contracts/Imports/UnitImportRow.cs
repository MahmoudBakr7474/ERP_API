namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>A row of the Units sheet as text, as read from the file. <paramref name="Row"/> is the sheet row number.</summary>
public sealed record UnitImportRow(int Row, string? Code, string? NameAr, string? NameEn, string? AllowsFraction);
