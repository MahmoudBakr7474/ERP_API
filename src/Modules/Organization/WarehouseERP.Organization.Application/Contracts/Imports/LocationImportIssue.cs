namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>
/// Why a row (or the file, at row 1) can't be imported. <paramref name="Field"/> is the column's key (branchCode,
/// warehouseCode, code, nameAr, nameEn), <paramref name="Code"/> a stable error code clients translate; the message is
/// English.
/// </summary>
public sealed record LocationImportIssue(int Row, string? Field, string Code, string Message, string? Value);
