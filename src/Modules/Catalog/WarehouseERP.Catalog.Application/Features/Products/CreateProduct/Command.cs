using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Sku, string NameAr, string? NameEn, Guid BaseUnitId, Guid? CategoryId, ProductType Type, TrackingMode TrackingMode);
