namespace WarehouseERP.Catalog.Application.Contracts.Products;

/// <param name="ConversionFactor">How many base units one of this unit holds; 1 for the base unit.</param>
/// <param name="IsBase">True for the unit the product's stock is kept in (Product.BaseUnitId).</param>
public sealed record ProductUnitResponse(
    Guid Id, Guid UnitId, decimal ConversionFactor, bool IsActive, string UnitCode, string UnitNameAr, string? UnitNameEn,
    bool IsBase, IReadOnlyList<ProductBarcodeResponse> Barcodes);
