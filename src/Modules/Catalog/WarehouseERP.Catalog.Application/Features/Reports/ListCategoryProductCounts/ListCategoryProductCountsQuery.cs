namespace WarehouseERP.Catalog.Application.Features.Reports.ListCategoryProductCounts;

/// <param name="IsActive">The categories' status; null lists every category.</param>
public sealed record ListCategoryProductCountsQuery(bool? IsActive = null);
