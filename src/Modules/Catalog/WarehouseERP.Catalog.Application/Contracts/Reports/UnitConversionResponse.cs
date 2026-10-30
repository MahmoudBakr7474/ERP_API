namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>One unit a product is handled in and how many base units it holds.</summary>
/// <param name="IsActive">The product unit's own status.</param>
/// <param name="BarcodeCount">Active barcodes on this unit.</param>
public sealed record UnitConversionResponse(
    Guid ProductId, string Sku, string ProductNameAr, string? ProductNameEn, bool ProductIsActive,
    Guid ProductUnitId, string UnitCode, string UnitNameAr, string? UnitNameEn, decimal ConversionFactor, bool IsBase,
    bool IsActive, string BaseUnitCode, string BaseUnitNameAr, string? BaseUnitNameEn, int BarcodeCount);
