namespace WarehouseERP.Catalog.Application.Features.Categories.UpdateCategory;

/// <param name="ParentId">The parent after the change; null moves the category to the top.</param>
/// <param name="RowVersion">Base64 row version from the last read; a stale one is a conflict.</param>
public sealed record UpdateCategoryCommand(Guid CategoryId, string NameAr, string? NameEn, Guid? ParentId, string RowVersion);
