using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Contracts.Products;

public sealed record ProductSummaryResponse(
    Guid Id, string Sku, string NameAr, string? NameEn, ProductType Type, TrackingMode TrackingMode, bool IsActive,
    Guid? CategoryId, Guid BaseUnitId);
