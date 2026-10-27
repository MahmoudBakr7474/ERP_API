namespace WarehouseERP.Catalog.Application.Features.Categories.ListCategories;

/// <param name="IsActive">Only active or only inactive categories; null for all.</param>
public sealed record ListCategoriesQuery(bool? IsActive);
