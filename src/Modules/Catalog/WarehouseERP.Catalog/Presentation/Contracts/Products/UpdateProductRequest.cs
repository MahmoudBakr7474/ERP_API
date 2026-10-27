namespace WarehouseERP.Catalog.Presentation.Contracts.Products;

/// <param name="CategoryId">The category after the change; null (or left out) takes the product out of any category.</param>
public sealed record UpdateProductRequest(string NameAr, string? NameEn, string RowVersion, Guid? CategoryId = null);
