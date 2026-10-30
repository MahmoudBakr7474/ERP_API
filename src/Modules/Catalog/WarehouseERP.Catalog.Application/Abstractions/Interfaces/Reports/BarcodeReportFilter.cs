using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Reports;

/// <param name="Search">Matches the product's SKU or either name, or part of the barcode.</param>
/// <param name="IsActive">The barcode's own status.</param>
public sealed record BarcodeReportFilter(
    string? Search, Guid? CategoryId, ProductType? Type, bool? ProductIsActive, BarcodeType? BarcodeType, bool? IsActive);
