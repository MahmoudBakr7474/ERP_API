using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Products;

/// <param name="RowVersion">Base64; send it back to update the product.</param>
public sealed record ProductDetailsResponse(
    Guid Id, string Sku, string NameAr, string? NameEn, Guid? CategoryId, Guid BaseUnitId, ProductType Type,
    TrackingMode TrackingMode, bool IsActive, string RowVersion, IReadOnlyList<ProductUnitResponse> Units);
