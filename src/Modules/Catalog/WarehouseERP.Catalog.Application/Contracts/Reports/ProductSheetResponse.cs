using WarehouseERP.Catalog.Application.Contracts.Products;

namespace WarehouseERP.Catalog.Application.Contracts.Reports;

/// <summary>Everything the product specification sheet prints: the product, its units with their barcodes, its last change.</summary>
public sealed record ProductSheetResponse(ProductMasterResponse Product, IReadOnlyList<ProductUnitResponse> Units, DateTime? UpdatedAtUtc);
