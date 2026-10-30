using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>
/// Why a row (or a sheet, at row 1) can't be imported. <paramref name="Field"/> is the column's key (sku, nameAr,
/// conversionFactor...), <paramref name="Code"/> a stable error code clients translate; the message is English.
/// </summary>
public sealed record CatalogImportIssue(CatalogImportSheet Sheet, int Row, string? Field, string Code, string Message, string? Value);
