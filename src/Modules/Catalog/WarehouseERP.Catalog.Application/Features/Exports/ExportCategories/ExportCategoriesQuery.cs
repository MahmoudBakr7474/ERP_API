namespace WarehouseERP.Catalog.Application.Features.Exports.ExportCategories;

/// <summary>Categories in tree order with their product counts (the category list and the products-by-category report).</summary>
/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportCategoriesQuery(bool? IsActive, int MaxRows);
