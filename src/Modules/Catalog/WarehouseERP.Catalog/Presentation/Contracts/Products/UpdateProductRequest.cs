namespace WarehouseERP.Catalog.Presentation.Contracts.Products;

public sealed record UpdateProductRequest(string NameAr, string? NameEn, string RowVersion);
