namespace WarehouseERP.Catalog.Presentation.Contracts.Categories;

public sealed record UpdateCategoryRequest(string NameAr, string? NameEn, Guid? ParentId, string RowVersion);
