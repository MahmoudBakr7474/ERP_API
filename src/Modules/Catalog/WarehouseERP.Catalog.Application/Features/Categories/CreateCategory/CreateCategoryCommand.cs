namespace WarehouseERP.Catalog.Application.Features.Categories.CreateCategory;

/// <param name="ParentId">The category this one sits under; null for a top-level category.</param>
public sealed record CreateCategoryCommand(string Code, string NameAr, string? NameEn, Guid? ParentId);
