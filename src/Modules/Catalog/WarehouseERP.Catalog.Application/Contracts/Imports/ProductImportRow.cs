namespace WarehouseERP.Catalog.Application.Contracts.Imports;

/// <summary>A row of the Products sheet as text. Type and tracking are enum names (StockItem, None...).</summary>
public sealed record ProductImportRow(
    int Row, string? Sku, string? NameAr, string? NameEn, string? BaseUnitCode, string? CategoryCode, string? Type, string? TrackingMode);
