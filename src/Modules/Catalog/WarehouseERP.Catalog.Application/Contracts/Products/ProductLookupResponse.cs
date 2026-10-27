using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Products;

/// <summary>A sellable item found by its barcode: the product, the unit the barcode is printed on, and the barcode.</summary>
/// <param name="ConversionFactor">How many base units one scanned item holds.</param>
public sealed record ProductLookupResponse(
    Guid ProductId, string Sku, string NameAr, string? NameEn, ProductType Type, TrackingMode TrackingMode,
    Guid ProductUnitId, Guid UnitId, string UnitCode, string UnitNameAr, string? UnitNameEn, decimal ConversionFactor,
    bool IsBaseUnit, Guid BarcodeId, string Barcode, BarcodeType BarcodeType);
