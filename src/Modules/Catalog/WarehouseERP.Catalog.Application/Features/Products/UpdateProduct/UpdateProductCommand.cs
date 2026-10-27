namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

/// <param name="CategoryId">The category after the change; null takes the product out of any category.</param>
/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateProductCommand(Guid ProductId, string NameAr, string? NameEn, Guid? CategoryId, string RowVersion);
