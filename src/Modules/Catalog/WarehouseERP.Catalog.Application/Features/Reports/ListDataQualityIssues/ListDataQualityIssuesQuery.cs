using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Features.Reports.ListDataQualityIssues;

/// <param name="Issue">One kind of issue, or every kind.</param>
/// <param name="Search">Matches the product's SKU or either name.</param>
/// <param name="ProductIsActive">Which products are checked; null checks all of them.</param>
public sealed record ListDataQualityIssuesQuery(
    int Page, int? PageSize, CatalogDataIssue? Issue = null, string? Search = null, bool? ProductIsActive = null) : IPagedQuery;
