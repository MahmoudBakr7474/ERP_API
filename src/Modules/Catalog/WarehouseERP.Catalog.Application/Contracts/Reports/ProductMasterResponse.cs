using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>A product as the product master report and its exports show it: names of its category and base unit.</summary>
/// <param name="UnitCount">Active units, the base unit included.</param>
/// <param name="BarcodeCount">Active barcodes on active units.</param>
public sealed record ProductMasterResponse(
    Guid Id, string Sku, string NameAr, string? NameEn, ProductType Type, TrackingMode TrackingMode, bool IsActive,
    string? CategoryCode, string? CategoryNameAr, string? CategoryNameEn,
    string BaseUnitCode, string BaseUnitNameAr, string? BaseUnitNameEn,
    int UnitCount, int BarcodeCount, DateTime CreatedAtUtc);
