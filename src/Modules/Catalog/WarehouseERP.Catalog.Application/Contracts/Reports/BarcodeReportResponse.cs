using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>A barcode with the product and unit it is printed on.</summary>
/// <param name="IsActive">The barcode's own status.</param>
public sealed record BarcodeReportResponse(
    Guid ProductId, string Sku, string ProductNameAr, string? ProductNameEn, string UnitCode, string UnitNameAr,
    string? UnitNameEn, decimal ConversionFactor, string Barcode, BarcodeType BarcodeType, bool IsActive);
