using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Domain;

namespace WarehouseERP.Catalog.Application.Features.Imports;

/// <summary>A Products row after checking.</summary>
/// <param name="BaseAllowsFraction">Whether the base unit (after this import) allows fractions: decides whole factors.</param>
public sealed record PlannedProduct(
    int Row, string? Sku, string? NameAr, string? NameEn, string? BaseUnitCode, string? CategoryCode, ProductType Type,
    TrackingMode Tracking, bool BaseAllowsFraction, Product? Existing, CatalogImportAction Action);
